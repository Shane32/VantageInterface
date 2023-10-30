using System.Diagnostics;

namespace VantageInterface;

public class VControl : IDisposable
{
    //private variables
    private readonly IVConnection _connection;
    private event Action<string?>? _gotText;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _connected => !_cancellationTokenSource.IsCancellationRequested;

    //properties
    public VControlExecute Execute { get; }
    public VControlGet Get { get; }
    public VControlSet Set { get; }

    //events
    public event EventHandler<VLoadEventArgs>? OnLoadUpdate;
    public event EventHandler<VLedEventArgs>? OnLedUpdate;
    public event EventHandler<VTaskEventArgs>? OnTaskUpdate;
    public event EventHandler<VButtonEventArgs>? OnButtonUpdate;
    public event EventHandler<VTemperatureSensorEventArgs>? OnTemperatureSensorUpdate;
    public event EventHandler<EventArgs>? OnDisconnected;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public VControl(IVConnection connection)
    {
        if (!connection.Connected)
            throw new ArgumentException("Connection must be connected.", nameof(connection));
        Execute = new VControlExecute(this);
        Get = new VControlGet(this);
        Set = new VControlSet(this);
        _connection = connection;
        _ = StartListening1Async();
        _ = StartListening2Async();

        async Task StartListening1Async()
        {
            // events get synchronized back to the caller's context
            try {
                await foreach (var txt in _connection.AsyncNotifications.WithCancellation(_cancellationTokenSource.Token).ConfigureAwait(true)) {
                    OnNext(txt);
                }
            } finally {
                if (!_cancellationTokenSource.IsCancellationRequested)
                    _cancellationTokenSource.Cancel();
                OnDisconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        async Task StartListening2Async()
        {
            // gotText gets called on a threadpool thread, so synchronous calls to WaitFor work properly
            try {
                await foreach (var txt in _connection.AsyncNotifications.WithCancellation(_cancellationTokenSource.Token).ConfigureAwait(false)) {
                    _gotText?.Invoke(txt);
                }
            } finally {
                if (!_cancellationTokenSource.IsCancellationRequested)
                    _cancellationTokenSource.Cancel();
                Debug.WriteLine("Connection closed");
                _gotText?.Invoke(null);
            }
        }
    }

    internal void WriteLine(string str)
    {
        WriteLineAsync(str, default).GetAwaiter().GetResult();
    }

    internal Task WriteLineAsync(string str, CancellationToken cancellationToken = default)
    {
        return _connection.WriteLineAsync(str, cancellationToken);
    }

    private void OnNext(string ret)
    {
        try {
            Debug.WriteLine($"Got some text: {ret}");
            VEventArgs? eventArgs = null;
            if (StartsWith("S:LOAD ") || StartsWith("R:GETLOAD ")) { //LOAD 123 55.0
                var (vid, percent) = ret.ParseLoad();
                var ev = new VLoadEventArgs(vid, percent);
                OnLoadUpdate?.Invoke(this, ev);
                eventArgs = ev;
            } else if (StartsWith("S:TASK ") || StartsWith("R:GETTASK ")) {
                var (vid, state) = ret.ParseTask();
                var ev = new VTaskEventArgs(vid, state);
                OnTaskUpdate?.Invoke(this, ev);
                eventArgs = ev;
            } else if (StartsWith("S:BTN ")) {
                var (vid, mode) = ret.ParseButton();
                var ev = new VButtonEventArgs(vid, mode);
                OnButtonUpdate?.Invoke(this, ev);
                eventArgs = ev;
            } else if (StartsWith("S:LED ") || StartsWith("R:GETLED ")) {
                var (vid, state) = ret.ParseLed();
                var ev = new VLedEventArgs(vid, state);
                OnLedUpdate?.Invoke(this, ev);
                eventArgs = ev;
            } else if (StartsWith("R:GETTHERMTEMP ")) {
                var (vid, sensor, temp) = ret.ParseThermTemp();
                var ev = new VTemperatureSensorEventArgs(vid, sensor, temp);
                OnTemperatureSensorUpdate?.Invoke(this, ev);
                eventArgs = ev;
            }
        } catch { }

        bool StartsWith(string value) => ret.StartsWith(value, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (!_cancellationTokenSource.IsCancellationRequested)
            _cancellationTokenSource.Cancel();
        GC.SuppressFinalize(this);
    }

    internal string WaitFor(string commandToSend, string commandToWaitFor)
    {
        if (!_connected)
            throw new ObjectDisposedException(nameof(VControl));
        if (!commandToWaitFor.EndsWith(" ", StringComparison.Ordinal))
            commandToWaitFor += " ";
        string? result = null;
        using var manualResetEvent = new ManualResetEvent(false);
        void task(string? command)
        {
            if (command == null) {
                _gotText -= task;
                Interlocked.MemoryBarrier();
                manualResetEvent.Set();
                return;
            } else {
                if (command.StartsWith(commandToWaitFor, StringComparison.Ordinal)) {
                    _gotText -= task;
                    result = command;
                    Interlocked.MemoryBarrier();
                    manualResetEvent.Set();
                }
            }
        }
        _gotText += task;
        if (!_connected) {
            _gotText -= task;
            throw new ObjectDisposedException(nameof(VControl));
        }
        WriteLine(commandToSend);
        manualResetEvent.WaitOne();
        Interlocked.MemoryBarrier();
        if (result == null)
            throw new ObjectDisposedException(nameof(VControl));
        return result;
    }

    internal async Task<string> WaitForAsync(string commandToSend, string commandToWaitFor, CancellationToken cancellationToken = default)
    {
        if (!commandToWaitFor.EndsWith(" ", StringComparison.Ordinal))
            commandToWaitFor += " ";
        var taskCompletionSource = new TaskCompletionSource<string>();

        void task(string? command)
        {
            if (command == null) {
                _gotText -= task;
                //run the callback on a separate thread
                Task.Run(() => {
                    //assuming that another thread has awaited taskCompletionSource.Task,
                    //  this will run the completion function synchronously
                    taskCompletionSource.SetException(new ObjectDisposedException(nameof(VControl)));
                }, default);
            } else {
                if (command.StartsWith(commandToWaitFor, StringComparison.Ordinal)) {
                    _gotText -= task;
                    //run the callback on a separate thread
                    Task.Run(() => {
                        //assuming that another thread has awaited taskCompletionSource.Task,
                        //  this will run that function synchronously
                        taskCompletionSource.SetResult(command);
                    }, default);
                }
            }
        }

        _gotText += task;
        if (!_connected) {
            _gotText -= task;
            throw new ObjectDisposedException(nameof(VControl));
        }
        await WriteLineAsync(commandToSend, cancellationToken).ConfigureAwait(false);
        return await taskCompletionSource.Task.ConfigureAwait(false);
    }
}
