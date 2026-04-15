namespace VantageServer;

public class Query
{
    public static async Task<House?> House(
        [FromServices] HouseManager houseManager,
        [Id] string houseIdentifier)
        => (await houseManager.GetHousesAsync()).TryGetValue(houseIdentifier, out var value) ? value : null;

    public static async Task<IEnumerable<House>> Houses([FromServices] HouseManager houseManager)
        => (await houseManager.GetHousesAsync()).Values;
}
