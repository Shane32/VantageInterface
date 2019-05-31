using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;

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

        //events
        public event Action<int, float> LoadUpdate;
        public event Action<int, float> LedUpdate;

        public VControl(string hostName) {
            _hostName = hostName;
            _tcpClient.LingerState = new LingerOption(true, 1);
        }

        // set-up
        public void Connect() {
            // connect to vantage system
            // set up asynchronous listener for lighting and button led changes
            _tcpClient.Connect(_hostName, 3001);
            _networkStream = _tcpClient.GetStream();
            _streamWriter = new StreamWriter(_networkStream, System.Text.Encoding.ASCII);
            _streamReader = new StreamReader(_networkStream, System.Text.Encoding.ASCII);

            _streamWriter.WriteLine("STATUS LOAD");
            _streamWriter.WriteLine("STATUS LED");
            _streamWriter.WriteLine("STATUS BTN");
            _streamWriter.WriteLine("STATUS TASK");
            _streamWriter.WriteLine("STATUS TEMP");
            _streamWriter.WriteLine("STATUS THERMFAN");
            _streamWriter.WriteLine("STATUS THERMOP");
            _streamWriter.WriteLine("STATUS THERMDAY");
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

            await _streamWriter.WriteLineAsync("STATUS LOAD");
            await _streamWriter.WriteLineAsync("STATUS LED");
            await _streamWriter.WriteLineAsync("STATUS BTN");
            await _streamWriter.WriteLineAsync("STATUS TASK");
            await _streamWriter.WriteLineAsync("STATUS TEMP");
            await _streamWriter.WriteLineAsync("STATUS THERMFAN");
            await _streamWriter.WriteLineAsync("STATUS THERMOP");
            await _streamWriter.WriteLineAsync("STATUS THERMDAY");
            await _streamWriter.WriteLineAsync("ECHO 0 INFUSION");
            await _streamWriter.FlushAsync();

            _connected = true;
            StartListener();
        }

        private async void StartListener() {
            while (_connected) {
                try {
                    var ret = await _streamReader.ReadLineAsync();
                    if (ret == null) return;
                    Debug.WriteLine($"Got some text: {ret}");
                    if (ret.StartsWith("S:LOAD ")) { //LOAD 123 55.0
                        string[] ret2 = ret.Split(' ');
                        int vid = int.Parse(ret2[1]);
                        float percent = float.Parse(ret2[2]);
                        LoadUpdate(vid, percent);
                    }
                }
                catch { }
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
        public void SetLoad(int vid, float percent) {
            //set a lighting load to a specified level
            _streamWriter.WriteLine($"LOAD {vid} {percent}.000000.0");
            _streamWriter.Flush();
        }

        public async Task SetLoadAsync(int vid, float percent) {
            await _streamWriter.WriteLineAsync($"LOAD {vid} {percent}.000000.0");
            await _streamWriter.FlushAsync();
        }

        public async Task<int> GetLoadAsync(int vid) {
            //retrieve the current level of a specified load
            await UpdateLoadAsync(vid);

            //wait for the level to be returned, and return it to the caller
            throw new NotSupportedException();
        }

        public void UpdateLoad(int vid) {
            //request that the current level of a specified load be updated
            _streamWriter.WriteLine($"GETLOAD {vid}");
            _streamWriter.Flush();
        }

        public async Task UpdateLoadAsync(int vid)
        {
            //request that the current level of a specified load be updated
            await _streamWriter.WriteLineAsync($"GETLOAD {vid}");
            await _streamWriter.FlushAsync();
        }

        // buttons & their leds
        public void PushButton(int vid, int pushType) {
            //pushType = 1: push, 2: release, 3: push & release
        }

        public Task<int> GetLedAsync(int vid) {
            throw new NotSupportedException();

        }

        public void UpdateLed(int vid) {

        }

        public Task UpdateLedAsync(int vid)
        {
            throw new NotSupportedException();
        }

    }
}
