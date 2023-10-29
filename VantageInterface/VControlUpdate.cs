namespace VantageInterface;

public class VControlUpdate
{
    private readonly VControl _control;

    internal VControlUpdate(VControl control)
    {
        _control = control;
    }

    public void Load(int vid)
    {
        //request that the current level of a specified load be updated
        _control.WriteLine($"GETLOAD {vid}");
    }

    public async Task LoadAsync(int vid)
    {
        //request that the current level of a specified load be updated
        await _control.WriteLineAsync($"GETLOAD {vid}").ConfigureAwait(false);
    }

    public void Led(int vid)
    {
        _control.WriteLine($"GETLED {vid}");
    }

    public Task LedAsync(int vid)
    {
        return _control.WriteLineAsync($"GETLED {vid}");
    }

    public void Task(int vid)
    {
        _control.WriteLine($"GETTASK {vid}");
    }

    public Task TaskAsync(int vid)
    {
        return _control.WriteLineAsync($"GETTASK {vid}");
    }

    public void Temperature(int vid)
    {
        _control.WriteLine($"GETTEMP {vid}");
    }

    public Task TemperatureAsync(int vid)
    {
        return _control.WriteLineAsync($"GETTEMP {vid}");
    }

    public void ThermostatCoolSetpoint(int vid)
    {
        _control.WriteLine($"GETTHERMTEMP {vid} COOL");
    }

    public Task ThermostatCoolSetpointAsync(int vid)
    {
        return _control.WriteLineAsync($"GETTHERMTEMP {vid} COOL");
    }

    public void ThermostatHeatSetpoint(int vid)
    {
        _control.WriteLine($"GETTHERMTEMP {vid} HEAT");
    }

    public Task ThermostatHeatSetpointAsync(int vid)
    {
        return _control.WriteLineAsync($"GETTHERMTEMP {vid} HEAT");
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
