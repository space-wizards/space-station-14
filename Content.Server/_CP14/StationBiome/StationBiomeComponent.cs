using Content.Shared.Parallax.Biomes;
using Robust.Shared.Prototypes;

namespace Content.Server.CrystallPunk.SpawnMapBiome;

/// <summary>
/// allows you to initialize a planet on a specific map at initialization time.
/// </summary>

[RegisterComponent, Access(typeof(StationBiomeSystem))]
public sealed partial class StationBiomeComponent : Component
{
    [DataField]
    public ProtoId<BiomeTemplatePrototype> Biome = "Grasslands";

    // If null, its random
    [DataField]
    public int? Seed = null;
}
