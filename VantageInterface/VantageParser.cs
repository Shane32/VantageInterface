namespace VantageInterface;

internal static class VantageParser
{
    public static (int vid, float percent) ParseLoad(this string value)
    {
        string[] ret2 = value.Split(' ');
        int vid = int.Parse(ret2[1], CultureInfo.InvariantCulture);
        float percent = float.Parse(ret2[2], CultureInfo.InvariantCulture);
        return (vid, percent);
    }

    public static (int vid, int newState) ParseTask(this string value)
    {
        string[] ret2 = value.Split(' ');
        int vid = int.Parse(ret2[1], CultureInfo.InvariantCulture);
        int newState = int.Parse(ret2[2], CultureInfo.InvariantCulture);
        return (vid, newState);
    }

    public static (int vid, ButtonModes mode) ParseButton(this string value)
    {
        string[] ret2 = value.Split(' ');
        int vid = int.Parse(ret2[1], CultureInfo.InvariantCulture);
        var mode = ret2[2] == "PRESS" ? ButtonModes.Press : ret2[2] == "RELEASE" ? ButtonModes.Release : throw new FormatException();
        return (vid, mode);
    }

    public static (int Vid, LedState State) ParseLed(this string value)
    {
        string[] ret2 = value.Split(' ');
        int vid = int.Parse(ret2[1], CultureInfo.InvariantCulture);
        int mode = int.Parse(ret2[2], CultureInfo.InvariantCulture);
        byte red1 = byte.Parse(ret2[3], CultureInfo.InvariantCulture);
        byte green1 = byte.Parse(ret2[4], CultureInfo.InvariantCulture);
        byte blue1 = byte.Parse(ret2[5], CultureInfo.InvariantCulture);
        byte red2 = byte.Parse(ret2[6], CultureInfo.InvariantCulture);
        byte blue2 = byte.Parse(ret2[7], CultureInfo.InvariantCulture);
        byte green2 = byte.Parse(ret2[8], CultureInfo.InvariantCulture);
        BlinkRates blinkRate;
        switch(ret2[9])
        {
            case "FAST": blinkRate = BlinkRates.Fast; break;
            case "MEDIUM": blinkRate = BlinkRates.Fast; break;
            case "SLOW": blinkRate = BlinkRates.Fast; break;
            case "VERYSLOW": blinkRate = BlinkRates.Fast; break;
            case "OFF": blinkRate = BlinkRates.Fast; break;
            default: throw new InvalidOperationException("Cannot parse blink rate");
        }
        return (vid, new LedState(mode, red1, blue1, green1, red2, blue2, green2, blinkRate));
    }

    public static (int Vid, TemperatureSensors Sensor, float Temperature) ParseThermTemp(this string value)
    {
        string[] ret2 = value.Split(' ');
        int vid = int.Parse(ret2[1], CultureInfo.InvariantCulture);
        TemperatureSensors sensor;
        switch(ret2[2])
        {
            case "COOL": sensor = TemperatureSensors.Cool; break;
            case "HEAT": sensor = TemperatureSensors.Heat; break;
            case "INDOOR": sensor = TemperatureSensors.Indoor; break;
            case "OUTDOOR": sensor = TemperatureSensors.Outdoor; break;
            default: throw new InvalidOperationException();
        }
        float temp = float.Parse(ret2[3], CultureInfo.InvariantCulture);
        return (vid, sensor, temp);
    }
}
