using GraphQL;
using VantageInterface;

namespace VantageServer.Types
{
    public class House
    {
        private readonly HouseConfiguration _houseConfiguration;
        private readonly VControl _control;
        public string Name => _houseConfiguration.Name;
        public string Id => _houseConfiguration.Id;
        public string IpAddress => _houseConfiguration.IPAddress;

        public House(HouseConfiguration houseConfig, VControl vControl)
        {
            _houseConfiguration = houseConfig;
            _control = vControl;
        }

        public async Task<Load> Load([Id] int id)
            => new Load(id, await _control.Get.LoadAsync(id));

        public async Task<List<Load>> Loads([Id] IEnumerable<int> ids)
        {
            var list = new List<Load>();
            foreach (var id in ids)
            {
                list.Add(new Load(id, await _control.Get.LoadAsync(id)));
            }
            return list;
        }
    }
}
