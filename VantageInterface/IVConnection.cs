namespace VantageInterface;

public interface IVConnection : IDisposable
{
    /// <summary>
    /// Indicates if the instance is connected to the Vantage processor.
    /// </summary>
    bool Connected { get; }

    /// <summary>
    /// Returns an <see cref="IObservable{T}"/> instance which can be used to listen for lines of text from the Vantage processor.
    /// The calls to <see cref="IObserver{T}.OnNext(T)"/> are not synchronized back to the caller's context.
    /// </summary>
    IObservable<string> Notifications { get; }

    /// <summary>
    /// Returns an <see cref="IAsyncEnumerable{T}"/> that can be used to listen for lines of text from the Vantage processor.
    /// </summary>
    IAsyncEnumerable<string> AsyncNotifications { get; }

    /// <summary>
    /// Sends a line of text to the connected Vantage processor.
    /// </summary>
    Task WriteLineAsync(string text, CancellationToken cancellationToken);
}
