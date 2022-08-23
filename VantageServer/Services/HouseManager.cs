using VantageInterface;

namespace VantageServer.Services
{
    public class HouseManager
    {
        public IEnumerable<House> Houses => HousesDictionary.Values;
        public Dictionary<string, House> HousesDictionary { get; } = new(StringComparer.Ordinal);
        //public IEnumerable<HouseMutation> HouseMutations => HouseMutationsDictionary.Values;

        public HouseManager(IOptions<IEnumerable<HouseConfiguration>> houseConfigurations)
        {
            foreach (var houseConfig in houseConfigurations.Value
                .Where(x => !string.IsNullOrEmpty(x.IPAddress) && !string.IsNullOrEmpty(x.Name) && !string.IsNullOrEmpty(x.Id)))
            {
                var house = new House(houseConfig, new VControl(houseConfig.IPAddress));
                HousesDictionary.Add(houseConfig.Id, house);
            }
        }
    }
}
