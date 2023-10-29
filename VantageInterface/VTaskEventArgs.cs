namespace VantageInterface;

public class VTaskEventArgs : VEventArgs
{
    public int State { get; }

    public VTaskEventArgs(int vid, int state)
        : base(vid, VEventType.TaskUpdate)
    {
        State = state;
    }
}
