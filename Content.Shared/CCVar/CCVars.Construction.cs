using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     The maximum number of construction ghosts allowed per tile.
    /// </summary>
    public static readonly CVarDef<int> ConstructionMaxGhostsPerTile =
        CVarDef.Create("construction.max_ghosts_per_tile", 6, CVar.SERVER | CVar.REPLICATED);
} 