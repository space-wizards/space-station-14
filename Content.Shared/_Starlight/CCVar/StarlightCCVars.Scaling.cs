using Robust.Shared.Configuration;

namespace Content.Shared.Starlight.CCVar;

public sealed partial class StarlightCCVars
{
    /// <summary>
    /// Decides how many people a security officer counts as for the purpose of the scaling system.
    /// 0 means they are not counted at all.
    /// </summary>
    public static readonly CVarDef<double> ScalingSecurityWeight =
        CVarDef.Create("scaling.security_weight", 1.0, CVar.SERVERONLY);

    /// <summary>
    /// Decides how many people jobs like NTCC Officers, Captain, etc., count as for the purpose of the scaling system.
    /// 0 means they are not counted at all.
    /// </summary>
    /// <remarks>
    /// REMEMBER: This is only factored in if they are recorded as crew on the station.
    /// As such, NTCC Consortium Officers spawned as part of naval training, etc., do not qualify.
    /// </remarks>
    public static readonly CVarDef<double> ScalingCentcomWeight =
        CVarDef.Create("scaling.centcom_weight", 1.0, CVar.SERVERONLY);

    /// <summary>
    /// Decides how many people a salvage specialist counts as for the purpose of the scaling system.
    /// 0 means they are not counted at all.
    /// </summary>
    public static readonly CVarDef<double> ScalingSalvageWeight =
        CVarDef.Create("scaling.salvage_weight", 0.25, CVar.SERVERONLY);

    /// <summary>
    /// Indicates the "base" population of indicated departments.
    /// If population is below, targets are scaled DOWN.
    /// If population is above, targets are scaled UP.
    /// </summary>
    public static readonly CVarDef<int> ScalingPopulationBase =
        CVarDef.Create("scaling.population_base", 10, CVar.SERVERONLY);

    /// <summary>
    /// Determines how large a portion of the mob's base thresholds are used in the scaling calculations.
    /// Put simply, this decides how large a percentage of mobs' healthpools get added (or removed) per player outside baseline.
    /// </summary>
    public static readonly CVarDef<double> ScalingHealthWeight =
        CVarDef.Create("scaling.health_weight", 0.05, CVar.SERVERONLY);
}