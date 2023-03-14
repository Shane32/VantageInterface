using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace VantageInterface
{
    public class VControlGet
    {
        private readonly VControl _control;

        internal VControlGet(VControl control)
        {
            _control = control;
        }

        public float Load(int vid)
        {
            //retrieve the current level of a specified load
            var ret = _control.WaitFor($"GETLOAD {vid}", $"R:GETLOAD {vid}");
            var (_, percent) = ret.ParseLoad();
            return percent;
        }

        public async Task<float> LoadAsync(int vid)
        {
            //retrieve the current level of a specified load
            var ret = await _control.WaitForAsync($"GETLOAD {vid}", $"R:GETLOAD {vid}").ConfigureAwait(false);
            var (_, percent) = ret.ParseLoad();
            return percent;
        }

        public LedState Led(int vid)
        {
            var retStr = _control.WaitFor($"GETLED {vid}", $"R:GETLED {vid}");
            var ret = retStr.ParseLed();
            return ret.State;
        }

        public async Task<LedState> LedAsync(int vid)
        {
            var retStr = await _control.WaitForAsync($"GETLED {vid}", $"R:GETLED {vid}").ConfigureAwait(false);
            var ret = retStr.ParseLed();
            return ret.State;
        }

        public int Task(int vid)
        {
            var retStr = _control.WaitFor($"GETTASK {vid}", $"R:GETTASK {vid}");
            var ret = retStr.ParseTask();
            return ret.newState;
        }

        public async Task<int> TaskAsync(int vid)
        {
            var retStr = await _control.WaitForAsync($"GETTASK {vid}", $"R:GETTASK {vid}").ConfigureAwait(false);
            var ret = retStr.ParseTask();
            return ret.newState;
        }

        public string Version()
        {
            var retStr = _control.WaitFor("VERSION", "R:VERSION");
            return retStr.Split(' ')[1];
        }

        public async Task<string> VersionAsync()
        {
            var retStr = await _control.WaitForAsync("VERSION", "R:VERSION").ConfigureAwait(false);
            return retStr.Split(' ')[1];
        }

        public float ThermostatCoolSetpoint(int vid)
        {
            var retStr = _control.WaitFor($"GETTHERMTEMP {vid} COOL", "GETTHERMTEMP");
            var parsed = retStr.ParseThermTemp();
            return parsed.Temperature;
        }

        public async Task<float> ThermostatCoolSetpointAsync(int vid)
        {
            var retStr = await _control.WaitForAsync($"GETTHERMTEMP {vid} COOL", "GETTHERMTEMP").ConfigureAwait(false);
            var parsed = retStr.ParseThermTemp();
            return parsed.Temperature;
        }

        public float ThermostatHeatSetpoint(int vid)
        {
            var retStr = _control.WaitFor($"GETTHERMTEMP {vid} HEAT", "GETTHERMTEMP");
            var parsed = retStr.ParseThermTemp();
            return parsed.Temperature;
        }

        public async Task<float> ThermostatHeatSetpointAsync(int vid)
        {
            var retStr = await _control.WaitForAsync($"GETTHERMTEMP {vid} HEAT", "GETTHERMTEMP").ConfigureAwait(false);
            var parsed = retStr.ParseThermTemp();
            return parsed.Temperature;
        }

        //=======todo (add parsing, return values, add to monitoring) ===========
        public void Temperature(int vid)
        {
            _control.WriteLine($"GETTEMP {vid}");
        }

        public Task TemperatureAsync(int vid)
        {
            return _control.WriteLineAsync($"GETTEMP {vid}");
        }

        public void ThermostatIndoorTemperature(int vid)
        {
            _control.WriteLine($"GETTHERMTEMP {vid} INDOOR");
        }

        public Task ThermostatIndoorTemperatureAsync(int vid)
        {
            return _control.WriteLineAsync($"GETTHERMTEMP {vid} INDOOR");
        }
        public void ThermostatOutdoorTemperature(int vid)
        {
            _control.WriteLine($"GETTHERMTEMP {vid} OUTDOOR");
        }

        public Task ThermostatOutdoorTemperatureAsync(int vid)
        {
            return _control.WriteLineAsync($"GETTHERMTEMP {vid} OUTDOOR");
        }
        public void ThermostatFanMode(int vid)
        {
            _control.WriteLine($"GETTHERMFAN {vid}");
        }

        public Task ThermostatFanModeAsync(int vid)
        {
            return _control.WriteLineAsync($"GETTHERMFAN {vid}");
        }

        public void ThermostatMode(int vid)
        {
            _control.WriteLine($"GETTHERMOP {vid}");
        }

        public Task ThermostatModeAsync(int vid)
        {
            return _control.WriteLineAsync($"GETTHERMOP {vid}");
        }

        public void ThermostatNightMode(int vid)
        {
            _control.WriteLine($"GETTHERMDAY {vid}");
        }

        public Task ThermostatNightModeAsync(int vid)
        {
            return _control.WriteLineAsync($"GETTHERMDAY {vid}");
        }
    }
}
