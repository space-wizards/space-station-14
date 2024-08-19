using Content.Shared.Destructible.Thresholds;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonLayers;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Salvage.Magnet;
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

    private readonly ProtoId<WeightedRandomPrototype> _asteroidOreWeights = "AsteroidOre";

    private readonly MinMax _asteroidOreCount = new(5, 7);

    public ISalvageMagnetOffering GetSalvageOffering(int seed)
    {
        var rand = new System.Random(seed);

        // Asteroid seed
        if (seed % 2 == 0)
        {
            var configId = _asteroidConfigs[rand.Next(_asteroidConfigs.Count)];
            var configProto =_proto.Index(configId);
            var layers = new Dictionary<string, int>();

            var data = new DungeonData();
            data.Apply(configProto.Data);

            var config = new DungeonConfig()
            {
                Data = data,
                Layers = new(configProto.Layers),
                MaxCount = configProto.MaxCount,
                MaxOffset = configProto.MaxOffset,
                MinCount = configProto.MinCount,
                MinOffset = configProto.MinOffset,
                ReserveTiles = configProto.ReserveTiles
            };

            var count = _asteroidOreCount.Next(rand);
            var weightedProto = _proto.Index(_asteroidOreWeights);
            for (var i = 0; i < count; i++)
            {
                var ore = weightedProto.Pick(rand);
                config.Layers.Add(_proto.Index<OreDunGenPrototype>(ore));

                var layerCount = layers.GetOrNew(ore);
                layerCount++;
                layers[ore] = layerCount;
            }

            return new AsteroidOffering
            {
                Id = configId,
                DungeonConfig = config,
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
