namespace VantageInterface;

public partial class VConnectionManager
{
    /// <summary>
    /// Represents a virtual connection.
    /// </summary>
    private class VConnectionInfo
    {
        /// <summary>
        /// The task that is connecting to the vantage processor; null only when
        /// the <see cref="VConnectionInfo"/> is disposed.
        /// </summary>
        private Task<IVConnection>? _vConnectionTask;
        /// <summary>
        /// Null initially; the connection to the vantage processor when connected.
        /// Transitions back to null upon dispose.
        /// </summary>
        private IVConnection? _vConnection;
        /// <summary>
        /// The number current acquisition count for this connection.
        /// </summary>
        private int _connectionCount;
        /// <summary>
        /// The last time an acquisition was released, indicating the last time
        /// the connection was in use after <see cref="_connectionCount"/> reaches zero.
        /// </summary>
        private DateTime _lastConnectedUtc = DateTime.UtcNow;

        public VConnectionInfo(Task<IVConnection> connectionTask)
        {
            _vConnectionTask = connectionTask;
        }

        public ValueTask<IVConnection> AcquireAsync()
            => AcquireIfNotDisposedAsync() ?? throw new ObjectDisposedException(nameof(VConnectionInfo));

        /// <summary>
        /// Returns the connection if it is available; otherwise returns a task that completes when
        /// the connection is made.  Returns null if this instance has already been disposed.
        /// </summary>
        public ValueTask<IVConnection>? AcquireIfNotDisposedAsync()
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
            // if the connection process has completed successfully, just return that instance
            if (vConnection != null)
                return new(new VConnectionMapper(vConnection, () => {
                    // when disposing, just reduce the connection count
                    lock (this) {
                        _connectionCount--;
                        _lastConnectedUtc = DateTime.UtcNow;
                    }
                }));
            // since we have not connected yet, see below code
            return new(Continue(vConnectionTask));

            async Task<IVConnection> Continue(Task<IVConnection> vConnectionTask)
            {
                try {
                    // wait for the connection to be completed
                    var vConnection = await vConnectionTask.ConfigureAwait(false);
                    // set _vConnection unless this ConnectionInfo has already been disposed
                    lock (this) {
                        if (_vConnectionTask != null)
                            _vConnection = vConnection;
                    }
                    // return the connection
                    return new VConnectionMapper(vConnection, () => {
                        // when disposing, just reduce the connection count
                        lock (this) {
                            _connectionCount--;
                            _lastConnectedUtc = DateTime.UtcNow;
                        }
                    });
                } catch {
                    // if failure during connection, immediately reduce the connection count and rethrow
                    lock (this) {
                        _connectionCount--;
                        _vConnectionTask = null;
                    }
                    throw;
                }
            }
        }

        /// <summary>
        /// Disposes of this instances if there are no active acquisitions.
        /// </summary>
        /// <returns>Indicates if this instance has been disposed.</returns>
        public bool TryRelease(TimeSpan after)
        {
            lock (this) {
                if (_vConnectionTask == null)
                    return true;
                if (_connectionCount == 0 && _lastConnectedUtc.Add(after) < DateTime.UtcNow) {
                    Dispose();
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Forces the active connection to be disposed.
        /// </summary>
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

            async void DisposeWhenDone(Task<IVConnection> task) => (await task.ConfigureAwait(false)).Dispose();
        }
    }
}
