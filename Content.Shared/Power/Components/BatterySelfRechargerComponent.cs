using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Power.Components;

/// <summary>
/// Self-recharging battery.
/// To be used in combination with <see cref="BatteryComponent"/>.
/// For <see cref="PredictedBatteryComponent"/> use <see cref="PredictedBatterySelfRechargerComponent"/> instead.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class BatterySelfRechargerComponent : Component
{
    /// <summary>
    /// Is the component currently enabled?
    /// </summary>
    [DataField]
    public bool AutoRecharge = true;

    /// <summary>
    /// At what rate does the entity automatically recharge? In watts.
    /// </summary>
    [DataField]
    public float AutoRechargeRate;

    /// <summary>
    /// How long should the entity stop automatically recharging if charge is used?
    /// </summary>
    [DataField]
    public TimeSpan AutoRechargePauseTime = TimeSpan.FromSeconds(0);

    /// <summary>
    /// Do not auto recharge if this timestamp has yet to happen, set for the auto recharge pause system.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextAutoRecharge = TimeSpan.FromSeconds(0);
}
