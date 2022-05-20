using GraphQL;
using VantageInterface;

namespace VantageServer.Types
{
    public class House
    {
        private VControl _control;
        public House(VControl control)
        {
            _control = control;
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
