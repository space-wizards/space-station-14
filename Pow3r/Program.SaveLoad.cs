using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Content.Server.Power.Pow3r;
using static Content.Server.Power.Pow3r.PowerState;

namespace Pow3r
{
    internal sealed partial class Program
    {
        private void LoadFromDisk()
        {
            if (!File.Exists("data.json"))
                return;

            var dat = JsonSerializer.Deserialize<DiskDat>(File.ReadAllBytes("data.json"), SerializerOptions);

            if (dat == null)
                return;

            _paused = dat.Paused;
            _nextId = dat.NextId;
            _currentSolver = dat.Solver;

            _state = new PowerState
            {
                Networks = dat.Networks.ToDictionary(n => n.Id, n => n),
                Supplies = dat.Supplies.ToDictionary(s => s.Id, s => s),
                Loads = dat.Loads.ToDictionary(l => l.Id, l => l),
                Batteries = dat.Batteries.ToDictionary(b => b.Id, b => b)
            };

            _displayLoads = dat.Loads.ToDictionary(n => n.Id, _ => new DisplayLoad());
            _displaySupplies = dat.Supplies.ToDictionary(n => n.Id, _ => new DisplaySupply());
            _displayBatteries = dat.Batteries.ToDictionary(n => n.Id, _ => new DisplayBattery());
            _displayNetworks = dat.Networks.ToDictionary(n => n.Id, _ => new DisplayNetwork());

            RefreshLinks();
        }

        private void SaveToDisk()
        {
            var data = new DiskDat
            {
                Paused = _paused,
                NextId = _nextId,
                Solver = _currentSolver,

                Loads = _state.Loads.Values.ToList(),
                Batteries = _state.Batteries.Values.ToList(),
                Networks = _state.Networks.Values.ToList(),
                Supplies = _state.Supplies.Values.ToList()
            };

            File.WriteAllBytes("data.json", JsonSerializer.SerializeToUtf8Bytes(data, SerializerOptions));
        }

        private sealed class DiskDat
        {
            public bool Paused;
            public int NextId;
            public int Solver;

            public List<Load> Loads;
            public List<Network> Networks;
            public List<Supply> Supplies;
            public List<Battery> Batteries;
        }
    }
}
