using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Pow3r
{
    internal sealed unsafe partial class Program
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

            var tempLoads = dat.Loads
                .ToDictionary(x => x.Id, x => new Load(x.Id)
                {
                    DesiredPower = x.Desired,
                    Enabled = x.Enabled
                });

            var tempSupplies = dat.Supplies
                .ToDictionary(x => x.Id,
                    x => new Supply(x.Id)
                    {
                        MaxSupply = x.MaxSupply,
                        Enabled = x.Enabled,
                        SupplyRampRate = x.SupplyRampRate,
                        SupplyRampTolerance = x.SupplyRampTolerance
                    });

            var tempBatteries = dat.Batteries.ToDictionary(x => x.Id,
                x => new Battery(x.Id)
                {
                    MaxPassthrough = x.MaxPassthrough,
                    Capacity = x.Capacity,
                    Enabled = x.Enabled,
                    MaxSupply = x.MaxSupply,
                    SupplyRampRate = x.RampRate,
                    SupplyRampTolerance = x.RampTolerance,
                    MaxChargeRate = x.MaxChargeRate
                });

            _loads.AddRange(tempLoads.Values);
            _supplies.AddRange(tempSupplies.Values);
            _batteries.AddRange(tempBatteries.Values);

            _networks.AddRange(dat.Networks.Select(n =>
            {
                var network = new Network(n.Id);
                network.Loads.AddRange(n.Loads.Select(l => tempLoads[l]));
                network.Supplies.AddRange(n.Supplies.Select(s => tempSupplies[s]));
                network.BatteriesLoading.AddRange(n.BatteriesLoading.Select(l => tempBatteries[l]));
                network.BatteriesSupplying.AddRange(n.BatteriesSupplying.Select(s => tempBatteries[s]));
                return network;
            }));

            RefreshLinks();
        }

        private void SaveToDisk()
        {
            var data = new DiskDat
            {
                Paused = _paused,
                NextId = _nextId,

                Loads = _loads.Select(l => new DiskLoad
                {
                    Id = l.Id,
                    Desired = l.DesiredPower,
                    Enabled = l.Enabled
                }).ToList(),

                Networks = _networks.Select(n => new DiskNetwork
                {
                    Id = n.Id,
                    Loads = n.Loads.Select(c => c.Id).ToList(),
                    Supplies = n.Supplies.Select(c => c.Id).ToList(),
                    BatteriesLoading = n.BatteriesLoading.Select(c => c.Id).ToList(),
                    BatteriesSupplying = n.BatteriesSupplying.Select(c => c.Id).ToList(),
                }).ToList(),

                Supplies = _supplies.Select(s => new DiskSupply
                {
                    Id = s.Id,
                    Enabled = s.Enabled,
                    MaxSupply = s.MaxSupply,
                    SupplyRampRate = s.SupplyRampRate,
                    SupplyRampTolerance = s.SupplyRampTolerance
                }).ToList(),

                Batteries = _batteries.Select(b => new DiskBattery
                {
                    Id = b.Id,
                    Enabled = b.Enabled,
                    Capacity = b.Capacity,
                    MaxPassthrough = b.MaxPassthrough,
                    MaxSupply = b.MaxSupply,
                    RampRate = b.SupplyRampRate,
                    RampTolerance = b.SupplyRampTolerance,
                    MaxChargeRate = b.MaxChargeRate
                }).ToList()
            };

            File.WriteAllBytes("data.json", JsonSerializer.SerializeToUtf8Bytes(data, SerializerOptions));
        }

        private sealed class DiskDat
        {
            public bool Paused;
            public int NextId;
            public List<DiskLoad> Loads;
            public List<DiskNetwork> Networks;
            public List<DiskSupply> Supplies;
            public List<DiskBattery> Batteries;
        }

        private sealed class DiskLoad
        {
            public int Id;

            public bool Enabled;
            public float Desired;
        }

        private sealed class DiskSupply
        {
            public int Id;

            public bool Enabled;
            public float MaxSupply;
            public float SupplyRampTolerance;
            public float SupplyRampRate;
        }

        private sealed class DiskBattery
        {
            public int Id;

            public bool Enabled;
            public float Capacity;
            public float MaxPassthrough;
            public float MaxChargeRate;
            public float MaxSupply;
            public float RampTolerance;
            public float RampRate;
        }

        private sealed class DiskNetwork
        {
            public int Id;

            public List<int> Loads;
            public List<int> Supplies;
            public List<int> BatteriesLoading = new();
            public List<int> BatteriesSupplying = new();
        }
    }
}
