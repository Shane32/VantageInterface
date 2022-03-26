using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;

namespace VantageInterface
{
    public class VControl : IDisposable, IObservable<VEventArgs>
    {
        //private variables
        private TcpClient _tcpClient = new TcpClient();
        private NetworkStream _networkStream;
        private StreamWriter _streamWriter;
        private StreamReader _streamReader;
        private volatile bool _connected;
        private string _hostName;
        private event Action<string> _gotText;

        //properties
        public VControlExecute Execute { get; }
        public VControlGet Get { get; }
        public VControlSet Set { get; }

        //events
        public event EventHandler<VLoadEventArgs> OnLoadUpdate;
        public event EventHandler<VLedEventArgs> OnLedUpdate;
        public event EventHandler<VTaskEventArgs> OnTaskUpdate;
        public event EventHandler<VButtonEventArgs> OnButtonUpdate;
        public event EventHandler<VTemperatureSensorEventArgs> OnTemperatureSensorUpdate;

        public VControl(string hostName) {
            _hostName = hostName;
            _tcpClient.LingerState = new LingerOption(true, 1);
            Execute = new VControlExecute(this);
            Get = new VControlGet(this);
            Set = new VControlSet(this);
        }

        // set-up
        public void Connect() {
            // connect to vantage system
            // set up asynchronous listener for lighting and button led changes
            _tcpClient.Connect(_hostName, 3001);
            _networkStream = _tcpClient.GetStream();
            _streamWriter = new StreamWriter(_networkStream, System.Text.Encoding.ASCII);
            _streamReader = new StreamReader(_networkStream, System.Text.Encoding.ASCII);

            //_streamWriter.WriteLine("STATUS LOAD");
            //_streamWriter.WriteLine("STATUS LED");
            //_streamWriter.WriteLine("STATUS BTN");
            //_streamWriter.WriteLine("STATUS TASK");
            //_streamWriter.WriteLine("STATUS TEMP");
            //_streamWriter.WriteLine("STATUS THERMFAN");
            //_streamWriter.WriteLine("STATUS THERMOP");
            //_streamWriter.WriteLine("STATUS THERMDAY");
            _streamWriter.WriteLine("STATUS ALL");
            _streamWriter.WriteLine("ECHO 0 INFUSION");
            _streamWriter.Flush();

            _connected = true;
            StartListener();
        }

        public async Task ConnectAsync() {
            await _tcpClient.ConnectAsync(_hostName, 3001);
            _networkStream = _tcpClient.GetStream();
            _streamWriter = new StreamWriter(_networkStream, System.Text.Encoding.ASCII);
            _streamReader = new StreamReader(_networkStream, System.Text.Encoding.ASCII);

            //await _streamWriter.WriteLineAsync("STATUS LOAD");
            //await _streamWriter.WriteLineAsync("STATUS LED");
            //await _streamWriter.WriteLineAsync("STATUS BTN");
            //await _streamWriter.WriteLineAsync("STATUS TASK");
            //await _streamWriter.WriteLineAsync("STATUS TEMP");
            //await _streamWriter.WriteLineAsync("STATUS THERMFAN");
            //await _streamWriter.WriteLineAsync("STATUS THERMOP");
            //await _streamWriter.WriteLineAsync("STATUS THERMDAY");
            await _streamWriter.WriteLineAsync("STATUS ALL");
            await _streamWriter.WriteLineAsync("ECHO 0 INFUSION");
            await _streamWriter.FlushAsync();

            _connected = true;
            StartListener();
        }

        internal void WriteLine(string str)
        {
            _streamWriter.WriteLine(str);
            _streamWriter.Flush();
        }

        internal async Task WriteLineAsync(string str)
        {
            await _streamWriter.WriteLineAsync(str);
            await _streamWriter.FlushAsync();
        }

        private async void StartListener() {
            try
            {
                while (_connected)
                {
                    try
                    {
                        var ret = await _streamReader.ReadLineAsync();
                        if (ret == null)
                        {
                            Debug.WriteLine("Connection closed gracefully");
                            return;
                        }
                        Debug.WriteLine($"Got some text: {ret}");
                        _gotText?.Invoke(ret);
                        VEventArgs eventArgs = null;
                        if (ret.StartsWith("S:LOAD ") || ret.StartsWith("R:GETLOAD "))
                        { //LOAD 123 55.0
                            var (vid, percent) = ret.ParseLoad();
                            var ev = new VLoadEventArgs(vid, percent);
                            OnLoadUpdate?.Invoke(this, ev);
                            eventArgs = ev;
                        }
                        else if (ret.StartsWith("S:TASK ") || ret.StartsWith("R:GETTASK "))
                        {
                            var (vid, state) = ret.ParseTask();
                            var ev = new VTaskEventArgs(vid, state);
                            OnTaskUpdate?.Invoke(this, ev);
                            eventArgs = ev;
                        }
                        else if (ret.StartsWith("S:BTN "))
                        {
                            var (vid, mode) = ret.ParseButton();
                            var ev = new VButtonEventArgs(vid, mode);
                            OnButtonUpdate?.Invoke(this, ev);
                            eventArgs = ev;
                        }
                        else if (ret.StartsWith("S:LED ") || ret.StartsWith("R:GETLED "))
                        {
                            var (vid, state) = ret.ParseLed();
                            var ev = new VLedEventArgs(vid, state);
                            OnLedUpdate?.Invoke(this, ev);
                            eventArgs = ev;
                        }
                        else if (ret.StartsWith("R:GETTHERMTEMP "))
                        {
                            var (vid, sensor, temp) = ret.ParseThermTemp();
                            var ev = new VTemperatureSensorEventArgs(vid, sensor, temp);
                            OnTemperatureSensorUpdate?.Invoke(this, ev);
                            eventArgs = ev;
                        }
                        if (eventArgs != null)
                        {
                            IObserver<VEventArgs>[] os;
                            lock (_observers)
                            {
                                os = _observers.ToArray();
                            }
                            foreach (var o in os)
                            {
                                o.OnNext(eventArgs);
                            }
                        }
                    }
                    catch { 
                        if (!_tcpClient.Connected)
                        {
                            Debug.WriteLine("disconnected");
                            IObserver<VEventArgs>[] os;
                            lock (_observers)
                            {
                                _connected = false;
                                os = _observers.ToArray();
                                _observers.Clear();
                            }
                            foreach (var o in os)
                            {
                                o.OnCompleted();
                            }
                        }
                    }
                }
            }
            finally
            {
                _gotText?.Invoke(null);
            }
        }
        
        public void Close() {
            // close connection to vantage system
            _tcpClient.Close();
            _connected = false;
        }

        public void Dispose() {
            Close();
        }

        internal string WaitFor(string commandToSend, string commandToWaitFor)
        {
            if (!_connected) throw new ObjectDisposedException(nameof(VControl));
            if (!commandToWaitFor.EndsWith(" ")) commandToWaitFor += " ";
            string result = null;
            bool isDisposed = false;
            using (ManualResetEvent manualResetEvent = new ManualResetEvent(false))
            {
                void task(string command)
                {
                    if (command == null)
                    {
                        _gotText -= task;
                        isDisposed = true;
                        Interlocked.MemoryBarrier();
                        manualResetEvent.Set();
                        return;
                    }
                    else
                    {
                        if (command.StartsWith(commandToWaitFor))
                        {
                            _gotText -= task;
                            result = command;
                            Interlocked.MemoryBarrier();
                            manualResetEvent.Set();
                        }
                    }
                }
                _gotText += task;
                if (!_connected)
                {
                    _gotText -= task;
                    throw new ObjectDisposedException(nameof(VControl));
                }
                WriteLine(commandToSend);
                manualResetEvent.WaitOne();
                Interlocked.MemoryBarrier();
                if (isDisposed) throw new ObjectDisposedException(nameof(VControl));
                return result;
            }
        }

        internal async Task<string> WaitForAsync(string commandToSend, string commandToWaitFor)
        {
            if (!commandToWaitFor.EndsWith(" ")) commandToWaitFor += " ";
            TaskCompletionSource<string> taskCompletionSource = new TaskCompletionSource<string>();
            void task(string command)
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
                    });
                }
                else
                {
                    if (command.StartsWith(commandToWaitFor))
                    {
                        _gotText -= task;
                        //run the callback on a separate thread
                        Task.Run(() =>
                        {
                            //assuming that another thread has awaited taskCompletionSource.Task,
                            //  this will run that function synchronously
                            taskCompletionSource.SetResult(command);
                        });
                    }
                }
            }
            _gotText += task;
            if (!_connected)
            {
                _gotText -= task;
                throw new ObjectDisposedException(nameof(VControl));
            }
            await WriteLineAsync(commandToSend);
            return await taskCompletionSource.Task;
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
}
