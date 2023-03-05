using Robust.Shared.Configuration;

namespace Content.Server._Citadel.Worldgen;

[CVarDefs]
public sealed class WorldgenCVars
{
    /// <summary>
    ///     Whether or not world generation is enabled.
    /// </summary>
    public static readonly CVarDef<bool> WorldgenEnabled =
        CVarDef.Create("citadel.worldgen.enabled", true, CVar.SERVERONLY);

    /// <summary>
    ///     The worldgen config to use.
    /// </summary>
    public static readonly CVarDef<string> WorldgenConfig =
        CVarDef.Create("citadel.worldgen.worldgen_config", "Default", CVar.SERVERONLY);

    /// <summary>
    ///     The maximum amount of time the GC can process, in ms.
    /// </summary>
    public static readonly CVarDef<int> GCMaximumTimeMs =
        CVarDef.Create("citadel.gc.maximum_time_ms", 5, CVar.SERVERONLY);
}

