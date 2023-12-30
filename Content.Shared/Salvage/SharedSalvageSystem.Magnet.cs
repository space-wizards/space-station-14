using Content.Shared.Procedural;
using Content.Shared.Salvage.Magnet;
using Robust.Shared.Prototypes;

namespace Content.Shared.Salvage;

public abstract partial class SharedSalvageSystem
{
    private readonly List<SalvageMapPrototype> _salvageMaps = new();

    private readonly List<ProtoId<DungeonConfigPrototype>> _asteroidConfigs = new()
    {
        "BlobAsteroid",
        "ClusterAsteroid",
        "SpindlyAsteroid",
        "SwissCheeseAsteroid"
    };

    public ISalvageMagnetOffering GetSalvageOffering(int seed)
    {
        var rand = new System.Random(seed);

        if (seed % 2 == 0)
        {
            var config = _asteroidConfigs[rand.Next(_asteroidConfigs.Count)];
            // TODO: Loot, need runtime layers.

            return new AsteroidOffering
            {
                DungeonConfig = _proto.Index<DungeonConfigPrototype>(config.Id),
            };
        }
        else
        {
            _salvageMaps.Clear();
            _salvageMaps.AddRange(_proto.EnumeratePrototypes<SalvageMapPrototype>());
            var mapIndex = rand.Next(_salvageMaps.Count);
            var map = _salvageMaps[mapIndex];

            return new SalvageOffering()
            {
                SalvageMap = map,
            };
        }
    }
}
