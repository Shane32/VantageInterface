namespace VantageInterface;

public class VButtonEventArgs : VEventArgs
{
    public ButtonModes Mode { get; }

    public VButtonEventArgs(int vid, ButtonModes action)
        : base(vid, VEventType.ButtonUpdate)
    {
        Mode = action;
    }
}
