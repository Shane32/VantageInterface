namespace VantageInterface;

public class VLedEventArgs : VEventArgs
{
    public int State { get; }
    public byte Red { get; }
    public byte Green { get; }
    public byte Blue { get; }
    public byte RedBlink { get; }
    public byte GreenBlink { get; }
    public byte BlueBlink { get; }
    public BlinkRates BlinkMode { get; }

    internal VLedEventArgs(int vid, LedState ledState)
        : this(vid, ledState.State, ledState.Red, ledState.Green, ledState.Blue,
              ledState.RedBlink, ledState.GreenBlink, ledState.BlueBlink, ledState.BlinkMode)
    {
    }

    public VLedEventArgs(int vid, int state, byte red, byte green, byte blue, byte redBlink, byte greenBlink, byte blueBlink, BlinkRates blinkMode)
        : base(vid, VEventType.LedUpdate)
    {
        State = state;
        Red = red;
        Green = green;
        Blue = blue;
        RedBlink = redBlink;
        GreenBlink = greenBlink;
        BlueBlink = blueBlink;
        BlinkMode = blinkMode;
    }
}
