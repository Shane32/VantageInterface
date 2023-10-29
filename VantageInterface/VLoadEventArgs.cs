namespace VantageInterface;

public class VLoadEventArgs : VEventArgs
{
    public float Percent { get; }

    public VLoadEventArgs(int vid, float value)
        : base(vid, VEventType.LoadUpdate)
    {
        Percent = value;
    }
}
