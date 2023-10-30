using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace VantageInterface;

public class VControl : IDisposable, IObservable<VEventArgs>, IObserver<string>
{
    //private variables
    private readonly IVConnection _connection;
    private readonly IDisposable _subscription;
    private volatile bool _connected;
    private event Action<string?>? _gotText;

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

    public VControl(IVConnection connection) {
        Execute = new VControlExecute(this);
        Get = new VControlGet(this);
        Set = new VControlSet(this);
        _connection = connection;
        _subscription = _connection.Notifications.Subscribe(this);
        _connected = true;
    }

    internal void WriteLine(string str)
    {
        WriteLineAsync(str, default).GetAwaiter().GetResult();
    }

    internal Task WriteLineAsync(string str, CancellationToken cancellationToken = default)
    {
        return _connection.WriteLineAsync(str, cancellationToken);
    }

    void IObserver<string>.OnError(Exception error)
        => throw new NotImplementedException();

    void IObserver<string>.OnCompleted()
    {
        try {
            Debug.WriteLine("Connection closed gracefully");
            _gotText?.Invoke(null);

            //                Debug.WriteLine("disconnected");
            //                IObserver<VEventArgs>[] os;
            //                lock (_observers)
            //                {
            //                    _connected = false;
            //                    os = _observers.ToArray();
            //                    _observers.Clear();
            //                }
            //                foreach (var o in os)
            //                {
            //                    o.OnCompleted();
            //                }

        } catch { }
    }

    void IObserver<string>.OnNext(string ret)
    {
        try {
            Debug.WriteLine($"Got some text: {ret}");
            _gotText?.Invoke(ret);
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
            if (eventArgs != null) {
                IObserver<VEventArgs>[] os;
                lock (_observers) {
                    os = _observers.ToArray();
                }
                foreach (var o in os) {
                    o.OnNext(eventArgs);
                }
            }
        } catch { }

        bool StartsWith(string value) => ret.StartsWith(value, StringComparison.Ordinal);
    }
    
    public void Dispose() {
        _subscription.Dispose();
        _connected = false;
        GC.SuppressFinalize(this);
    }

    internal string WaitFor(string commandToSend, string commandToWaitFor)
    {
        if (!_connected) throw new ObjectDisposedException(nameof(VControl));
        if (!commandToWaitFor.EndsWith(" ", StringComparison.Ordinal)) commandToWaitFor += " ";
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
        if (!commandToWaitFor.EndsWith(" ", StringComparison.Ordinal)) commandToWaitFor += " ";
        var taskCompletionSource = new TaskCompletionSource<string>();

        void task(string? command)
        {
            if (command == null)
            {
                _gotText -= task;
                //run the callback on a separate thread
                Task.Run(() =>
                {
                    //assuming that another thread has awaited taskCompletionSource.Task,
                    //  this will run the completion function synchronously
                    taskCompletionSource.SetException(new ObjectDisposedException(nameof(VControl)));
                }, default);
            }
            else
            {
                if (command.StartsWith(commandToWaitFor, StringComparison.Ordinal))
                {
                    _gotText -= task;
                    //run the callback on a separate thread
                    Task.Run(() =>
                    {
                        //assuming that another thread has awaited taskCompletionSource.Task,
                        //  this will run that function synchronously
                        taskCompletionSource.SetResult(command);
                    }, default);
                }
            }
        }

        _gotText += task;
        if (!_connected)
        {
            _gotText -= task;
            throw new ObjectDisposedException(nameof(VControl));
        }
        await WriteLineAsync(commandToSend, cancellationToken).ConfigureAwait(false);
        return await taskCompletionSource.Task.ConfigureAwait(false);
    }

    private readonly List<IObserver<VEventArgs>> _observers = new List<IObserver<VEventArgs>>();
    public IDisposable Subscribe(IObserver<VEventArgs> observer)
    {
        lock (_observers)
        {
            if (!_connected)
                throw new ObjectDisposedException(nameof(VControl));
            _observers.Add(observer);
        }
        return new Subscription(() =>
        {
            lock (_observers)
            {
                _observers.Remove(observer);
            }
        });
    }

    private class Subscription : IDisposable
    {
        private readonly Action _disposeAction;
        public Subscription(Action disposeAction)
        {
            _disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
        }

        public void Dispose()
            => _disposeAction();
    }
}
