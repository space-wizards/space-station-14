using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

/// <summary>
/// Denotes an entity that will receive appearance updates for the state of its PowerNetworkBattery.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class PowerNetworkBatteryVisualsComponent : Component
{
    /// <summary>
    /// The amount of time to wait between updates.
    /// </summary>
    [DataField(serverOnly: true)]
    public TimeSpan VisualsChangeDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The number of distinct charge levels a battery has.
    /// 0 is empty, and (NumChargeLevels - 1) is full.
    /// </summary>
    [DataField(serverOnly: true)]
    public int NumChargeLevels = 7;

    /// <summary>
    /// The last charge level of the battery.
    /// The initial visuals for this entity should match an empty state for correct behaviour.
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
    /// The initial visuals for this entity should match a disconnected state for correct behaviour.
    /// </summary>
    [ViewVariables]
    public PowerNetworkBatteryChargeCapabilities LastChargeCapabilities = PowerNetworkBatteryChargeCapabilities.Neither;

    /// <summary>
    /// The next time that the visuals should be updated.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextUpdateTime;

    /// <summary>
    /// The prefix used for the RSI states of the sprite layers indicating the charge level of the SMES.
    /// </summary>
    [DataField]
    public string ChargeLevelPrefix = "charge";

    /// <summary>
    /// If false, charge level 0 will make the ChargeLayer invisible. If true, charge level 0 will make the ChargeLayer set to its 0 state.
    /// </summary>
    [DataField]
    public bool ChargeLevelZeroVisible = true;

    /// <summary>
    /// The prefix used for the RSI states of the sprite layers indicating the input state of the SMES.
    /// Will be suffixed with "discharging", "charging", or "still"
    /// </summary>
    [DataField]
    public string ChargeStatePrefix = "";
}
