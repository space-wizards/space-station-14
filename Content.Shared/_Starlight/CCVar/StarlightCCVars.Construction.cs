using Robust.Shared.Configuration;

namespace Content.Shared.Starlight.CCVar;

public sealed partial class StarlightCCVars
{
    /// <summary>
    ///     The maximum number of construction ghosts allowed per tile.
    /// </summary>
    public static readonly CVarDef<int> ConstructionMaxGhostsPerTile =
        CVarDef.Create("construction.max_ghosts_per_tile", 6, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);
} 