using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Server.Power.EntitySystems;

namespace Content.Server.Power.Components;

/// <summary>
/// Denotes an entity that will receive appearance updates for the state of its PowerNetworkBattery.
/// </summary>
[RegisterComponent, Access(typeof(PowerNetworkBatteryVisualsSystem)), AutoGenerateComponentPause]
public sealed partial class PowerNetworkBatteryVisualsComponent : Component
{
    /// <summary>
    /// The amount of time to wait between updates.
    /// </summary>
    [DataField]
    public TimeSpan VisualsChangeDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The number of distinct charge levels a battery has.
    /// 0 is empty, and (NumChargeLevels - 1) is full.
    /// </summary>
    [DataField]
    public int NumChargeLevels = 7;

    /// <summary>
    /// The last charge level of the battery.
    /// The visuals for this entity should match an empty state for correct behaviour.
    /// </summary>
    [ViewVariables]
    public int LastChargeLevel = 0;

    /// <summary>
    /// Whether the battery is being charged/discharged/neither.
    /// The initial visuals for this entity should match a stable charge for correct behaviour.
    /// </summary>
    [ViewVariables]
    public ChargeState LastChargeState = ChargeState.Still;

    /// <summary>
    /// Whether the battery can be charged and/or discharged.
    /// The visuals for this entity should match a disconnected state for correct behaviour.
    /// </summary>
    [ViewVariables]
    public PowerNetworkBatteryChargeCapabilities LastChargeCapabilities = PowerNetworkBatteryChargeCapabilities.Neither;

    /// <summary>
    /// The next time that the visuals should be updated.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextUpdateTime;
}
