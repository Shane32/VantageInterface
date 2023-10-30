namespace VantageInterface;

public struct LedState
{
    public int State { get; set; }
    public byte Red { get; set; }
    public byte Green { get; set; }
    public byte Blue { get; set; }
    public byte RedBlink { get; set; }
    public byte GreenBlink { get; set; }
    public byte BlueBlink { get; set; }
    public BlinkRates BlinkMode { get; set; }

    public LedState(int state, byte red, byte green, byte blue, byte redBlink, byte greenBlink, byte blueBlink, BlinkRates blinkMode)
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
