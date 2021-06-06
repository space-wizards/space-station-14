using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Pow3r
{
    internal sealed partial class Program
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            IncludeFields = true,
            Converters = {new NodeIdJsonConverter()}
        };

        private void LoadFromDisk()
        {
            if (!File.Exists("data.json"))
                return;

            var dat = JsonSerializer.Deserialize<DiskDat>(File.ReadAllBytes("data.json"), SerializerOptions);

            if (dat == null)
                return;

            _paused = dat.Paused;
            _nextId = dat.NextId;

            _networks = dat.Networks.ToDictionary(n => n.Id, n => n);
            _supplies = dat.Supplies.ToDictionary(s => s.Id, s => s);
            _loads = dat.Loads.ToDictionary(l => l.Id, l => l);
            _batteries = dat.Batteries.ToDictionary(b => b.Id, b => b);

            RefreshLinks();
        }

        private void SaveToDisk()
        {
            var data = new DiskDat
            {
                Paused = _paused,
                NextId = _nextId,

                Loads = _loads.Values.ToList(),
                Batteries = _batteries.Values.ToList(),
                Networks = _networks.Values.ToList(),
                Supplies = _supplies.Values.ToList()
            };

            File.WriteAllBytes("data.json", JsonSerializer.SerializeToUtf8Bytes(data, SerializerOptions));
        }

        private sealed class DiskDat
        {
            public bool Paused;
            public int NextId;

            public List<Load> Loads;
            public List<Network> Networks;
            public List<Supply> Supplies;
            public List<Battery> Batteries;
        }
    }
}
