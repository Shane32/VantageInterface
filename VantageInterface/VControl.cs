using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace VantageInterface
{
    public class VControl : IDisposable
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
        public event Action<int, float> LoadUpdate;
        public event Action<int, LedState> LedUpdate;
        public event Action<int, int> TaskUpdate;
        public event Action<int, ButtonModes> ButtonUpdate;

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
                        if (ret.StartsWith("S:LOAD ") || ret.StartsWith("R:GETLOAD "))
                        { //LOAD 123 55.0
                            var (vid, percent) = ret.ParseLoad();
                            LoadUpdate?.Invoke(vid, percent);
                        }
                        else if (ret.StartsWith("S:TASK "))
                        {
                            var (vid, state) = ret.ParseTask();
                            TaskUpdate?.Invoke(vid, state);
                        }
                        else if (ret.StartsWith("S:BTN "))
                        {
                            var (vid, mode) = ret.ParseButton();
                            ButtonUpdate?.Invoke(vid, mode);
                        }
                        else if (ret.StartsWith("S:LED "))
                        {
                            var led = ret.ParseLed();
                            LedUpdate?.Invoke(led.Vid, led.State);
                        }
                    }
                    catch { 
                        if (!_tcpClient.Connected)
                        {
                            Debug.WriteLine("disconnected");
                            _connected = false;
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

        // lighting loads

        // buttons & their leds
        public void PushButton(int vid, int pushType) {
            //pushType = 1: push, 2: release, 3: push & release
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
                    Task.Run(() =>
                    {
                        try
                        {
                            taskCompletionSource.SetException(new ObjectDisposedException(nameof(VControl)));
                        }
                        catch { }
                    });
                }
                else
                {
                    if (command.StartsWith(commandToWaitFor))
                    {
                        _gotText -= task;
                        Task.Run(() =>
                        {
                            try
                            {
                                taskCompletionSource.SetResult(command);
                            }
                            catch { }
                        });
                    }
                }
            }
            _gotText += task;
            await WriteLineAsync(commandToSend);
            return await taskCompletionSource.Task;
        }
    }
}
