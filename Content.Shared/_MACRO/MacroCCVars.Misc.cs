using Robust.Shared.Configuration;

namespace Content.Shared._MACRO.CCVar;

// Used for miscellaneous, highly-specific game CVars - e.g. variables that interact with only a single, narrow-purpose system.

public sealed partial class MacroCCVars
{
    /// <summary>
    ///     How many times an entity must be consumed before they gib
    ///     12 by default, set to 0 to disable.
    /// </summary>
    public static readonly CVarDef<int> ConsumptionGibThreshold =
        CVarDef.Create("macrocosm.consumption.gib_threshold", 12, CVar.NOTIFY | CVar.REPLICATED | CVar.SERVER);
}
