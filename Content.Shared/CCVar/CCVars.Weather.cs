using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Maximum number of entities processed per tick by the weather entity effect system.
    /// Higher values apply effects faster but may cause lag spikes on large maps.
    /// The system spreads effect application across multiple ticks to stay within this budget.
    /// </summary>
    public static readonly CVarDef<int> WeatherMaxAffectedPerTick =
        CVarDef.Create("weather.max_affected_per_tick", 100, CVar.SERVERONLY);

    /// <summary>
    /// Maximum number of tiles scanned per tick during the gathering phase.
    /// Controls how fast the system discovers exposed entities on grids.
    /// </summary>
    public static readonly CVarDef<int> WeatherMaxTilesScannedPerTick =
        CVarDef.Create("weather.max_tiles_scanned_per_tick", 200, CVar.SERVERONLY);
}
