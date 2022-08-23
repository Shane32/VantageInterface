using Microsoft.Extensions.Options;
using VantageServer.Configuration;
using VantageServer.Types;

namespace VantageServer
{
    public class Query
    {
        public static House? House(
            [FromServices] HouseManager houseManager,
            [Id] string houseIdentifier)
            => houseManager.HousesDictionary.TryGetValue(houseIdentifier, out var value) ? value : null;

        public static IEnumerable<House> Houses([FromServices] HouseManager houseManager)
            => houseManager.Houses;
    }
}
