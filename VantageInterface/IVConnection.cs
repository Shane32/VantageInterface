namespace VantageInterface;

public interface IVConnection : IDisposable
{
    IObservable<string> Notifications { get; }
    IAsyncEnumerable<string> AsyncNotifications { get; }
    Task WriteLineAsync(string text, CancellationToken cancellationToken);
}
