using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shane32.AsyncResetEvents;
using static System.Net.Mime.MediaTypeNames;

namespace VantageInterface
{
    public class VConnection : IDisposable
    {
        private bool _connected;
        private bool _disposed;
        private TcpClient? _client;
        private NetworkStream? _networkStream;
        private CancellationTokenSource _cts = new();
        private readonly object _sync = new();
        private readonly AsyncDelegatePump _delegatePump = new();

        public async Task ConnectAsync(string host, CancellationToken cancellationToken)
        {
            if (_connected)
                throw new InvalidOperationException("Connection already made with this instance");
            if (_disposed)
                throw new ObjectDisposedException(nameof(VConnection));

            var client = new TcpClient();
            try {
#if NET5_0_OR_GREATER
                await client.ConnectAsync(host, 1234, cancellationToken).ConfigureAwait(false);
#else
                await client.ConnectAsync(host, 1234).ConfigureAwait(false);
#endif
                var bytes = Encoding.ASCII.GetBytes("STATUS ALL\r\nECHO 0 INFUSION\r\n");
                var stream = client.GetStream();
                await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);

                _client = client;
                _networkStream = stream;
                _cts = new();
                _connected = true;
            } catch {
                client.Dispose();
                throw;
            }
        }

        private Task WriteLineAsync(string text, CancellationToken cancellationToken)
        {
            if (!_connected)
                throw new InvalidOperationException("Make connection with ConnectAsync first");
            if (_disposed)
                throw new ObjectDisposedException(nameof(VConnection));
            cancellationToken.ThrowIfCancellationRequested();

            var bytes = Encoding.ASCII.GetBytes(text + "\r\n");
            var sendTask = _delegatePump.SendAsync(async () => {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(VConnection));
                cancellationToken.ThrowIfCancellationRequested();
                await _networkStream!.WriteAsync(bytes, 0, bytes.Length, _cts.Token).ConfigureAwait(false);
                await _networkStream!.FlushAsync(_cts.Token).ConfigureAwait(false);
            });

            if (!sendTask.IsCompleted && cancellationToken.CanBeCanceled) {
#if NET6_0_OR_GREATER
                return sendTask.WaitAsync(cancellationToken);
#else
                return Task.WhenAny(sendTask, Task.Delay(-1, cancellationToken)).Unwrap();
#endif
            } else {
                return sendTask;
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
