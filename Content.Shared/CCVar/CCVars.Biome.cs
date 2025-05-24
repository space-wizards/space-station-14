using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Load range for biomes. Set this higher than PVS so server can have some time to load in before the client arrives.
    /// </summary>
    public static readonly CVarDef<float> BiomeLoadRange =
        CVarDef.Create("biome.load_range", 20f, CVar.SERVERONLY);

    /// <summary>
    /// Time allocation (ms) for how long biomes are allowed to load.
    /// </summary>
    public static readonly CVarDef<float> BiomeLoadTime =
        CVarDef.Create("biome.load_time", 0.03f, CVar.SERVERONLY);
}
