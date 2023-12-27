using Content.Shared.Procedural;
using Content.Shared.Salvage.Magnet;

namespace Content.Shared.Salvage;

public abstract partial class SharedSalvageSystem
{
    private readonly List<SalvageMapPrototype> _salvageMaps = new();

    public ISalvageMagnetOffering GetSalvageOffering(int seed)
    {
        var rand = new System.Random(seed);

        if (seed % 2 == 0)
        {
            var config = _proto.Index<DungeonConfigPrototype>("Asteroid");
            return new AsteroidOffering
            {
                DungeonConfig = config,
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
