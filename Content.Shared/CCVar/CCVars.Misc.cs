using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<bool> HolidaysEnabled = CVarDef.Create("holidays.enabled", true, CVar.SERVERONLY);
    public static readonly CVarDef<bool> BrandingSteam = CVarDef.Create("branding.steam", false, CVar.CLIENTONLY);
    public static readonly CVarDef<int> EntityMenuGroupingType = CVarDef.Create("entity_menu", 0, CVar.CLIENTONLY);

    /// <summary>
    ///     Should we pre-load all of the procgen atlasses.
    /// </summary>
    public static readonly CVarDef<bool> ProcgenPreload =
        CVarDef.Create("procgen.preload", true, CVar.SERVERONLY);

    /// <summary>
    ///     Enabled: Cloning has 70% cost and reclaimer will refuse to reclaim corpses with souls. (For LRP).
    ///     Disabled: Cloning has full biomass cost and reclaimer can reclaim corpses with souls. (Playtested and balanced for MRP+).
    /// </summary>
    public static readonly CVarDef<bool> BiomassEasyMode =
        CVarDef.Create("biomass.easy_mode", true, CVar.SERVERONLY);

    /// <summary>
    ///     A scale factor applied to a grid's bounds when trying to find a spot to randomly generate an anomaly.
    /// </summary>
    public static readonly CVarDef<float> AnomalyGenerationGridBoundsScale =
        CVarDef.Create("anomaly.generation_grid_bounds_scale", 0.6f, CVar.SERVERONLY);

    /// <summary>
    ///     How long a client can go without any input before being considered AFK.
    /// </summary>
    public static readonly CVarDef<float> AfkTime =
        CVarDef.Create("afk.time", 60f, CVar.SERVERONLY);

    /// <summary>
    ///     Flavor limit. This is to ensure that having a large mass of flavors in
    ///     some food object won't spam a user with flavors.
    /// </summary>
    public static readonly CVarDef<int>
        FlavorLimit = CVarDef.Create("flavor.limit", 10, CVar.SERVERONLY);

    public static readonly CVarDef<string> DestinationFile =
        CVarDef.Create("autogen.destination_file", "", CVar.SERVER | CVar.SERVERONLY);

    /// <summary>
    ///     Whether uploaded files will be stored in the server's database.
    ///     This is useful to keep "logs" on what files admins have uploaded in the past.
    /// </summary>
    public static readonly CVarDef<bool> ResourceUploadingStoreEnabled =
        CVarDef.Create("netres.store_enabled", true, CVar.SERVER | CVar.SERVERONLY);

    /// <summary>
    ///     Numbers of days before stored uploaded files are deleted. Set to zero or negative to disable auto-delete.
    ///     This is useful to free some space automatically. Auto-deletion runs only on server boot.
    /// </summary>
    public static readonly CVarDef<int> ResourceUploadingStoreDeletionDays =
        CVarDef.Create("netres.store_deletion_days", 30, CVar.SERVER | CVar.SERVERONLY);

    /// <summary>
    ///     If a server update restart is pending, the delay after the last player leaves before we actually restart. In seconds.
    /// </summary>
    public static readonly CVarDef<float> UpdateRestartDelay =
        CVarDef.Create("update.restart_delay", 20f, CVar.SERVERONLY);

    /// <summary>
    ///     If fire alarms should have all access, or if activating/resetting these
    ///     should be restricted to what is dictated on a player's access card.
    ///     Defaults to true.
    /// </summary>
    public static readonly CVarDef<bool> FireAlarmAllAccess =
        CVarDef.Create("firealarm.allaccess", true, CVar.SERVERONLY);

    /// <summary>
    ///     Time between play time autosaves, in seconds.
    /// </summary>
    public static readonly CVarDef<float>
        PlayTimeSaveInterval = CVarDef.Create("playtime.save_interval", 900f, CVar.SERVERONLY);

    /// <summary>
    ///     The maximum amount of time the entity GC can process, in ms.
    /// </summary>
    public static readonly CVarDef<int> GCMaximumTimeMs =
        CVarDef.Create("entgc.maximum_time_ms", 5, CVar.SERVERONLY);

    public static readonly CVarDef<bool> GatewayGeneratorEnabled =
        CVarDef.Create("gateway.generator_enabled", true);

    public static readonly CVarDef<string> TippyEntity =
        CVarDef.Create("tippy.entity", "Tippy", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     The number of seconds that must pass for a single entity to be able to point at something again.
    /// </summary>
    public static readonly CVarDef<float> PointingCooldownSeconds =
        CVarDef.Create("pointing.cooldown_seconds", 0.5f, CVar.SERVERONLY);
}
