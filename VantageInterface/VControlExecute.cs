namespace VantageInterface;

public class VControlExecute
{
    private readonly VControl _control;

    internal VControlExecute(VControl control)
    {
        _control = control;
    }

    public void Task(int vid, TaskModes mode)
    {
        _control.WriteLine($"TASK {vid} {mode.ToString().ToUpper(CultureInfo.InvariantCulture)}");
    }

    public Task TaskAsync(int vid, TaskModes mode)
    {
        return _control.WriteLineAsync($"TASK {vid} {mode.ToString().ToUpper(CultureInfo.InvariantCulture)}");
    }

    public void Button(int vid, ButtonModes mode)
    {
        switch (mode)
        {
            case ButtonModes.Press:
                _control.WriteLine($"BTNPRESS {vid}");
                break;
            case ButtonModes.Release:
                _control.WriteLine($"BTNRELEASE {vid}");
                break;
            case ButtonModes.PressRelease:
                _control.WriteLine($"BTN {vid}");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    public Task ButtonAsync(int vid, ButtonModes mode)
    {
        switch (mode)
        {
            case ButtonModes.Press:
                return _control.WriteLineAsync($"BTNPRESS {vid}");
            case ButtonModes.Release:
                return _control.WriteLineAsync($"BTNRELEASE {vid}");
            case ButtonModes.PressRelease:
                return _control.WriteLineAsync($"BTN {vid}");
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    public string Command(string commandName, params string[] parameters)
    {
        var str = commandName;
        if (parameters != null && parameters.Length > 0)
        {
            str += string.Join(" ", parameters);
        }
        return _control.WaitFor(str, "R:" + commandName);
    }

    public Task<string> CommandAsync(string commandName, params string[] parameters)
    {
        var str = commandName;
        if (parameters != null && parameters.Length > 0)
        {
            str += string.Join(" ", parameters);
        }
        return _control.WaitForAsync(str, "R:" + commandName);
    }
}
