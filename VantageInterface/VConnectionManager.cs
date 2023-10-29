namespace VantageInterface;

/// <summary>
/// Multiplexes connections to the same Vantage host.
/// </summary>
public partial class VConnectionManager : IDisposable
{
    private readonly Dictionary<string, VConnectionInfo> _connections = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _disposedValue;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public VConnectionManager()
    {
        PeriodicCheckAsync();
    }

    /// <summary>
    /// Periodically checks for connections which have been unused for at least 30 seconds
    /// and disconnects them.
    /// </summary>
    private async void PeriodicCheckAsync()
    {
        while (!_cancellationTokenSource.IsCancellationRequested) {
            await Task.Delay(60000, _cancellationTokenSource.Token).ConfigureAwait(false);
            lock (_connections) {
                List<string>? toRemove = null;
                foreach (var keyValuePair in _connections) {
                    if (keyValuePair.Value.TryRelease(TimeSpan.FromSeconds(30))) {
                        toRemove ??= new();
                        toRemove.Add(keyValuePair.Key);
                    }
                }
                if (toRemove != null) {
                    foreach (var host in toRemove) {
                        _connections.Remove(host);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Acquires a connection to the specified host.
    /// Dispose the returned connection when finished to release.
    /// </summary>
    public ValueTask<IVConnection> AcquireAsync(string host, CancellationToken cancellationToken)
    {
        lock (_connections) {
            if (_cancellationTokenSource.IsCancellationRequested) {
                throw new ObjectDisposedException(nameof(VConnectionManager));
            }
            // look for an existing connection
            if (_connections.TryGetValue(host, out var info)) {
                var ret = info.AcquireIfNotDisposedAsync();
                if (ret.HasValue)
                    return ret.Value;
            }
            // create a new connection
            info = new VConnectionInfo(CreateNewConnectionAsync(host, cancellationToken));
            _connections[host] = info;
            // the returned task will complete when the connection is established
            // if the connection fails, the connection will be removed from the dictionary
            // and the error thrown
            return info.AcquireAsync();
        }
    }

    /// <summary>
    /// Creates a new connection to the specified host.
    /// </summary>
    protected virtual async Task<IVConnection> CreateNewConnectionAsync(string host, CancellationToken cancellationToken)
        => await VConnection.ConnectAsync(host, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Maps a virtual connection to a real connection.
    /// Disposing of the virtual connection releases the acquired connection without disposing it.
    /// </summary>
    private class VConnectionMapper : IVConnection
    {
        private readonly IVConnection _vConnection;
        private Action? _disposeAction;

        public VConnectionMapper(IVConnection vConnection, Action disposeAction)
        {
            _vConnection = vConnection;
            _disposeAction = disposeAction;
        }

        public IObservable<string> Notifications => _vConnection.Notifications;

        public IAsyncEnumerable<string> AsyncNotifications => _vConnection.AsyncNotifications;

        public Task WriteLineAsync(string text, CancellationToken cancellationToken) => _vConnection.WriteLineAsync(text, cancellationToken);

        public void Dispose() => Interlocked.Exchange(ref _disposeAction, null)?.Invoke();
    }

    /// <summary>
    /// Releases the unmanaged resources used by the class and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue) {
            if (disposing) {
                if (!_cancellationTokenSource.IsCancellationRequested)
                    _cancellationTokenSource.Cancel();
                lock (_connections) {
                    foreach (var info in _connections.Values) {
                        info.Dispose();
                    }
                    _connections.Clear();
                }
            }
            _disposedValue = true;
        }
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
