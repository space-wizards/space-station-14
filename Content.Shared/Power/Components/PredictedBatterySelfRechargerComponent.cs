using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Power.Components;

/// <summary>
/// Self-recharging battery.
/// To be used in combination with <see cref="PredictedBatteryComponent"/>.
/// For <see cref="BatteryComponent"/> use <see cref="BatterySelfRechargerComponent"/> instead.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class PredictedBatterySelfRechargerComponent : Component
{
    /// <summary>
    /// At what rate does the entity automatically recharge? In watts.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables]
    public float AutoRechargeRate;

    /// <summary>
    /// How long should the entity stop automatically recharging if a charge is used?
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AutoRechargePauseTime = TimeSpan.Zero;

    /// <summary>
    /// Do not auto recharge if this timestamp has yet to happen, set for the auto recharge pause system.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField, ViewVariables]
    public TimeSpan? NextAutoRecharge = TimeSpan.FromSeconds(0);
}
