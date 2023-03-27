using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VantageInterface;

public class VConnectionManager : IDisposable
{
    private readonly Dictionary<string, VConnectionInfo> _connections = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public VConnectionManager()
    {
        PeriodicCheckAsync();
    }

    private async void PeriodicCheckAsync()
    {
        while (!_cancellationTokenSource.IsCancellationRequested) {
            await Task.Delay(60000, _cancellationTokenSource.Token);
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

    public ValueTask<IVConnection> AcquireAsync(string host, CancellationToken cancellationToken)
    {
        lock (_connections) {
            if (_cancellationTokenSource.IsCancellationRequested) {
                throw new ObjectDisposedException(nameof(VConnectionManager));
            }
            if (_connections.TryGetValue(host, out var info)) {
                var ret = info.Acquire();
                if (ret.HasValue)
                    return ret.Value;
            }
            info = new VConnectionInfo(host, cancellationToken);
            _connections.Add(host, info);
            return info.Acquire()!.Value;
        }
    }

    public void Dispose()
    {
        if (!_cancellationTokenSource.IsCancellationRequested)
            _cancellationTokenSource.Cancel();
        lock (_connections) {
            foreach (var info in _connections.Values) {
                info.Dispose();
            }
            _connections.Clear();
        }
    }

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

        public Task WriteLineAsync(string text, CancellationToken cancellationToken) => _vConnection.WriteLineAsync(text, cancellationToken);

        public void Dispose() => Interlocked.Exchange(ref _disposeAction, null)?.Invoke();
    }

    private class VConnectionInfo
    {
        private Task<IVConnection>? _vConnectionTask;
        private IVConnection? _vConnection;
        private int _connectionCount;
        private DateTime _lastConnectedUtc = DateTime.UtcNow;

        public VConnectionInfo(string host, CancellationToken cancellationToken)
        {
            _vConnectionTask = ConnectAsync(host, cancellationToken);

            static async Task<IVConnection> ConnectAsync(string host, CancellationToken cancellationToken)
                => await VConnection.ConnectAsync(host, cancellationToken);
        }

        public ValueTask<IVConnection>? Acquire()
        {
            Task<IVConnection> vConnectionTask;
            IVConnection? vConnection;
            lock (this) {
                if (_vConnectionTask == null)
                    return null;
                vConnectionTask = _vConnectionTask;
                vConnection = _vConnection;
                _connectionCount++;
            }
            if (vConnection != null)
                return new(new VConnectionMapper(vConnection, () => {
                    lock (this) {
                        _connectionCount--;
                        _lastConnectedUtc = DateTime.UtcNow;
                    }
                }));
            return new(Continue(vConnectionTask));

            async Task<IVConnection> Continue(Task<IVConnection> vConnectionTask)
            {
                try {
                    var vConnection = await vConnectionTask;
                    lock (this) {
                        if (_vConnectionTask != null)
                            _vConnection = vConnection;
                    }
                    return new VConnectionMapper(vConnection, () => {
                        lock (this) {
                            _connectionCount--;
                            _lastConnectedUtc = DateTime.UtcNow;
                        }
                    });
                } catch {
                    lock (this) {
                        _connectionCount--;
                        _vConnectionTask = null;
                    }
                    throw;
                }
            }
        }

        public bool TryRelease(TimeSpan after)
        {
            lock (this) {
                if (_vConnectionTask == null)
                    return true;
                if (_connectionCount == 0 && _lastConnectedUtc.Add(after) < DateTime.UtcNow) {
                    _vConnectionTask.Dispose();
                    _vConnectionTask = null;
                    return true;
                }
                return false;
            }
        }

        public void Dispose()
        {
            lock (this) {
                if (_vConnection != null) {
                    _vConnection.Dispose();
                } else if (_vConnectionTask != null) {
                    DisposeWhenDone(_vConnectionTask);
                }
                _vConnectionTask = null;
                _vConnection = null;
            }

            async void DisposeWhenDone(Task<IVConnection> task) => (await task).Dispose();
        }
    }
}
