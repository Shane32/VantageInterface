namespace VantageInterface;

public class VControlSet
{
    private readonly VControl _control;

    internal VControlSet(VControl control)
    {
        _control = control;
    }

    public void Load(int vid, float percent, float seconds = 0)
    {
        //set a lighting load to a specified level
        if (seconds == 0)
        {
            _control.WriteLine($"LOAD {vid} {percent:0}");
        }
        else
        {
            _control.WriteLine($"RAMPLOAD {vid} {percent:0} {seconds:0.0}");
        }
    }

    public Task LoadAsync(int vid, float percent, float seconds = 0)
    {
        if (seconds == 0)
        {
            return _control.WriteLineAsync($"LOAD {vid} {percent:0}");
        }
        else
        {
            return _control.WriteLineAsync($"RAMPLOAD {vid} {percent:0} {seconds:0.0}");
        }
    }

    public void Led(int vid, byte red, byte green, byte blue, byte redBlink, byte greenBlink, byte blueBlink, BlinkRates blinkRate)
    {
        _control.WriteLine($"LED {vid} {red} {green} {blue} {redBlink} {greenBlink} {blueBlink} {blinkRate.ToString().ToLower(CultureInfo.InvariantCulture)}");
    }

    public Task LedAsync(int vid, byte red, byte green, byte blue, byte redBlink, byte greenBlink, byte blueBlink, BlinkRates blinkRate)
    {
        return _control.WriteLineAsync($"LED {vid} {red} {green} {blue} {redBlink} {greenBlink} {blueBlink} {blinkRate.ToString().ToLower(CultureInfo.InvariantCulture)}");
    }

    public void ThermostatCoolSetpoint(int vid, float value)
    {
        _control.WriteLine($"THERMTEMP {vid} COOL {value:0.0}");
    }

    public Task ThermostatCoolSetpointAsync(int vid, float value)
    {
        return _control.WriteLineAsync($"THERMTEMP {vid} COOL {value:0.0}");
    }

    public void ThermostatHeatSetpoint(int vid, float value)
    {
        _control.WriteLine($"THERMTEMP {vid} HEAT {value:0.0}");
    }

    public Task ThermostatHeatSetpointAsync(int vid, float value)
    {
        return _control.WriteLineAsync($"THERMTEMP {vid} HEAT {value:0.0}");
    }

    public void ThermostatFanMode(int vid, bool on)
    {
        _control.WriteLine($"THERMFAN {vid} {(on ? "ON" : "AUTO")}");
    }

    public Task ThermostatFanModeAsync(int vid, bool on)
    {
        return _control.WriteLineAsync($"THERMFAN {vid} {(on ? "ON" : "AUTO")}");
    }

    public void ThermostatMode(int vid, ThermostatModes mode)
    {
        _control.WriteLine($"THERMOP {vid} {mode.ToString().ToUpper(CultureInfo.InvariantCulture)}");
    }

    public Task ThermostatModeAsync(int vid, ThermostatModes mode)
    {
        return _control.WriteLineAsync($"THERMOP {vid} {mode.ToString().ToUpper(CultureInfo.InvariantCulture)}");
    }

    public void ThermostatNightMode(int vid, bool value)
    {
        _control.WriteLine($"THERMDAY {vid} {(value ? "NIGHT" : "DAY")}");
    }

    public Task ThermostatNightModeAsync(int vid, bool value)
    {
        return _control.WriteLineAsync($"THERMDAY {vid} {(value ? "NIGHT" : "DAY")}");
    }

    public void Variable(int vid, int value)
    {
        _control.WriteLine($"VARIABLE {vid} {value}");
    }

    public Task VariableAsync(int vid, int value)
    {
        return _control.WriteLineAsync($"VARIABLE {vid} {value}");
    }

    public void Variable(int vid, string value)
    {
        _control.WriteLine($"VARIABLE {vid} \"{value.Replace("\"","\"\"")}\"");
    }

    public Task VariableAsync(int vid, string value)
    {
        return _control.WriteLineAsync($"VARIABLE {vid} \"{value.Replace("\"", "\"\"")}\"");
    }

    public void BlindPosition(int vid, int value)
    {
        _control.WriteLine($"BLIND {vid} POS {value}");
    }

    public Task BlindPositionAsync(int vid, int value)
    {
        return _control.WriteLineAsync($"BLIND {vid} POS {value}");
    }
}
