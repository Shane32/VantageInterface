using System.Net.Sockets;
using System.Text;
using Shane32.AsyncResetEvents;

namespace VantageInterface;

/// <summary>
/// Represents a connection to a Vantage processor, capable of sending and receiving lines of text.
/// </summary>
public sealed class VConnection : IObservable<string>, IDisposable, IVConnection
{
    private readonly TcpClient _client;
    private readonly NetworkStream _networkStream;
    private CancellationTokenSource? _cts = new();
    private readonly CancellationToken _ctsToken;
    private readonly AsyncDelegatePump _delegatePump = new();
    private readonly ConcurrentSet<IObserver<string>> _listeners = new();

    private VConnection(TcpClient tcpClient, NetworkStream networkStream)
    {
        _ctsToken = _cts.Token;
        _client = tcpClient;
        _networkStream = networkStream;
        ListenerAsync();
    }

    /// <summary>
    /// Listens to responses over the TCP connection, and broadcasts each line of text received.
    /// </summary>
    private async void ListenerAsync()
    {
        // monitoring the cancellationtoken is not necessary as disposing the class will dispose the stream
        try {
            using var reader = new StreamReader(_networkStream, Encoding.UTF8, false, 8192, true);
            while (!_ctsToken.IsCancellationRequested) {
                // note: because ConfigureAwait(false) vs true, listener.OnNext will not be synchronized
                // back to the caller's context
#if NET7_0_OR_GREATER
                string? line = await reader.ReadLineAsync(_ctsToken).ConfigureAwait(false);
#else
                string? line = await reader.ReadLineAsync().ConfigureAwait(false);
#endif

                if (line == null) {
                    // End of stream, break the loop
                    break;
                }

                foreach (var listener in _listeners) {
                    listener.OnNext(line);
                }
            }
        } finally {
            Dispose();
            foreach (var listener in _listeners) {
                listener.OnCompleted();
            }
        }
    }

    private bool _disposed => _ctsToken.IsCancellationRequested;

    /// <summary>
    /// Initializes a new connection to a Vantage processor.
    /// </summary>
    public static async Task<VConnection> ConnectAsync(string host, CancellationToken cancellationToken)
    {
        var client = new TcpClient();
        try {
#if NET5_0_OR_GREATER
            await client.ConnectAsync(host, 3001, cancellationToken).ConfigureAwait(false);
#else
            await client.ConnectAsync(host, 3001).WaitAsync(cancellationToken).ConfigureAwait(false);
#endif
            var bytes = Encoding.ASCII.GetBytes("STATUS ALL\r\nECHO 0 INFUSION\r\n");
            var stream = client.GetStream();
#pragma warning disable CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'
            await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
#pragma warning restore CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'

            return new VConnection(client, stream);
        } catch {
            client.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Sends a line of text to the connected Vantage processor.
    /// </summary>
    public Task WriteLineAsync(string text, CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(VConnection));
        cancellationToken.ThrowIfCancellationRequested();

        var bytes = Encoding.ASCII.GetBytes(text + "\r\n");
        var sendTask = _delegatePump.SendAsync(async () => {
            if (_disposed)
                throw new ObjectDisposedException(nameof(VConnection));
            cancellationToken.ThrowIfCancellationRequested();
#pragma warning disable CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'
            await _networkStream!.WriteAsync(bytes, 0, bytes.Length, _ctsToken).ConfigureAwait(false);
#pragma warning restore CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'
            await _networkStream!.FlushAsync(_ctsToken).ConfigureAwait(false);
        }, cancellationToken);

        return sendTask.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Disposes of the connection and notifies all listeners of the disconnection.
    /// </summary>
    public void Dispose()
    {
        var cts = Interlocked.Exchange(ref _cts, null);
        try {
            cts?.Cancel();
            cts?.Dispose();
        } catch { }
        _networkStream.Dispose();
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Returns an <see cref="IObservable{T}"/> instance which can be used to listen for lines of text from the Vantage processor.
    /// The calls to <see cref="IObserver{T}.OnNext(T)"/> are not synchronized back to the caller's context.
    /// </summary>
    public IObservable<string> Notifications => this;

    IDisposable IObservable<string>.Subscribe(IObserver<string> observer)
    {
        _listeners.Add(observer);
        return new Disposer(this, observer);
    }

    /// <summary>
    /// Returns an <see cref="IAsyncEnumerable{T}"/> that can be used to listen for lines of text from the Vantage processor.
    /// </summary>
    public IAsyncEnumerable<string> AsyncNotifications => Notifications.ToAsyncEnumerable();

    private class Disposer : IDisposable
    {
        private VConnection? _connection;
        private readonly IObserver<string> _observer;

        public Disposer(VConnection connection, IObserver<string> observer)
        {
            _connection = connection;
            _observer = observer;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _connection, null)?._listeners.Remove(_observer);
        }
    }
}
