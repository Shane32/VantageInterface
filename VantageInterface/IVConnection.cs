using System;
using System.Threading;
using System.Threading.Tasks;

namespace VantageInterface;

public interface IVConnection : IDisposable
{
    IObservable<string> Notifications { get; }
    Task WriteLineAsync(string text, CancellationToken cancellationToken);
}
