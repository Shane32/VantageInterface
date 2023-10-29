namespace VantageInterface;

public abstract class VEventArgs : EventArgs
{
    public int Vid { get; }
    public VEventType Type { get; }

    protected VEventArgs(int vid, VEventType type)
    {
        Vid = vid;
        Type = type;
    }
}
