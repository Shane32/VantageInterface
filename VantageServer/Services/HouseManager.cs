using VantageInterface;

namespace VantageServer.Services;

public class HouseManager : IDisposable
{
    //public IEnumerable<House> Houses => HousesDictionary.Values;
    //public Dictionary<string, House> HousesDictionary { get; } = new(StringComparer.Ordinal);
    private readonly Lazy<Task<IDictionary<string, House>>> _lazyHouses;
    private CancellationTokenSource? _cancellationTokenSource;

    public HouseManager(VConnectionManager vConnectionManager, IOptions<IEnumerable<HouseConfiguration>> houseConfigurations, IHttpContextAccessor httpContextAccessor)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None);
        var token = _cancellationTokenSource.Token;
        _lazyHouses = new(async () => {
            token.ThrowIfCancellationRequested();
            var houses = houseConfigurations.Value
                .Where(x => !string.IsNullOrEmpty(x.IPAddress) && !string.IsNullOrEmpty(x.Name) && !string.IsNullOrEmpty(x.Id));
            var housesDictionary = new Dictionary<string, House>();
            foreach (var houseConfig in houses) {
                var connection = await vConnectionManager.AcquireAsync(houseConfig.IPAddress, token);
                token.Register(() => connection.Dispose());
                var house = new House(houseConfig, new VControl(connection));
                housesDictionary.Add(houseConfig.Id, house);
            }
            return housesDictionary;
        });
    }

    public Task<IDictionary<string, House>> GetHousesAsync()
        => _lazyHouses.Value;

    public void Dispose()
    {
        var cts = Interlocked.Exchange(ref _cancellationTokenSource, null);
        cts?.Cancel(); // trigger all registered cancellations
        cts?.Dispose(); // disposing of a linked cts will unregister it from the source
    }
}
