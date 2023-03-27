using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shane32.AsyncResetEvents;

namespace VantageInterface;

public class VConnection : IObservable<string>, IDisposable, IVConnection
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

    private async void ListenerAsync()
    {
        // monitoring the cancellationtoken is not necessary as disposing the class will dispose the stream
        try {
            using var reader = new StreamReader(_networkStream, Encoding.UTF8, leaveOpen: true);
            while (!_ctsToken.IsCancellationRequested) {
                string? line = await reader.ReadLineAsync().ConfigureAwait(false);

                if (line == null) {
                    // End of stream, break the loop
                    break;
                }

                OnLineReceived(line);
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

    protected virtual void OnLineReceived(string text)
    {
    }

    private bool _disposed => _ctsToken.IsCancellationRequested;

    public static async Task<VConnection> ConnectAsync(string host, CancellationToken cancellationToken)
    {
        var client = new TcpClient();
        try {
#if NET5_0_OR_GREATER
            await client.ConnectAsync(host, 1234, cancellationToken).ConfigureAwait(false);
#else
            await client.ConnectAsync(host, 1234).WaitAsync(cancellationToken).ConfigureAwait(false);
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
        });

        return sendTask.WaitAsync(cancellationToken);
    }

    public void Dispose()
    {
        var cts = Interlocked.Exchange(ref _cts, null);
        try {
            cts?.Cancel();
            cts?.Dispose();
        } catch { }
        _networkStream?.Dispose();
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }

    public IObservable<string> Notifications => this;

    IDisposable IObservable<string>.Subscribe(IObserver<string> observer)
    {
        _listeners.Add(observer);
        return new Disposer(this, observer);
    }

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
