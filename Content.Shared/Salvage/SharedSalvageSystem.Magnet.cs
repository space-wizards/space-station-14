using Content.Shared.Procedural;
using Content.Shared.Procedural.PostGeneration;
using Content.Shared.Random.Helpers;
using Content.Shared.Salvage.Magnet;
using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

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

        // Asteroid seed
        if (seed % 2 == 0)
        {
            var config = _asteroidConfigs[rand.Next(_asteroidConfigs.Count)];
            var configProto = _proto.Index(config);
            var layers = new Dictionary<string, int>();

            // If we ever add more random layers will need to Next on these.
            foreach (var layer in configProto.Layers)
            {
                switch (layer)
                {
                    case BiomeDunGen:
                        rand.Next();
                        break;
                    case BiomeMarkerLayerDunGen marker:
                        for (var i = 0; i < marker.Count; i++)
                        {
                            var proto = _proto.Index(marker.MarkerTemplate).Pick(rand);
                            var layerCount = layers.GetOrNew(proto);
                            layerCount++;
                            layers[proto] = layerCount;
                        }
                        break;
                }
            }

            return new AsteroidOffering
            {
                DungeonConfig = configProto,
                MarkerLayers = layers,
            };
        }

        // Salvage map seed
        _salvageMaps.Clear();
        _salvageMaps.AddRange(_proto.EnumeratePrototypes<SalvageMapPrototype>());
        _salvageMaps.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));
        var mapIndex = rand.Next(_salvageMaps.Count);
        var map = _salvageMaps[mapIndex];

        return new SalvageOffering()
        {
            SalvageMap = map,
        };
    }
}
