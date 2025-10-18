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
            _currentSolver = dat.Solver;

            _state = new PowerState
            {
                Networks = GenIdStorage.FromEnumerable(dat.Networks.Select(n => (n.Id, n))),
                Supplies = GenIdStorage.FromEnumerable(dat.Supplies.Select(s => (s.Id, s))),
                Loads = GenIdStorage.FromEnumerable(dat.Loads.Select(l => (l.Id, l))),
                Batteries = GenIdStorage.FromEnumerable(dat.Batteries.Select(b => (b.Id, b)))
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
            public int Solver;

            public List<Load> Loads;
            public List<Network> Networks;
            public List<Supply> Supplies;
            public List<Battery> Batteries;
        }
    }
}
