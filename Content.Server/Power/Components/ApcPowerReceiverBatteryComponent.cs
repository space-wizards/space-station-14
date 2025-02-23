using Content.Server.Power.EntitySystems;
using Content.Shared.Power.EntitySystems;

namespace Content.Server.Power.Components;

/// <summary>
/// Attached to APC powered entities that possess a rechargeable internal battery.
/// If external power is interrupted, the entity will draw power from this battery instead. 
/// Requires <see cref="ApcPowerReceiverComponent"/> and <see cref="BatteryComponent"/> to function.
/// </summary>
[RegisterComponent]
[Access([typeof(PowerNetSystem), typeof(SharedPowerReceiverSystem)])]
public sealed partial class ApcPowerReceiverBatteryComponent : Component
{
    /// <summary>
    /// Indicates whether power is currently being drawn from the battery.
    /// </summary>
    [DataField]
    public bool Enabled = false;

    /// <summary>
    /// The passive load the entity places on the APC power network.
    /// If not connected to an active APC power network, this amount
    /// of power is drained from the battery every second.
    /// </summary>
    [DataField]
    public float IdleLoad { get; set; } = 5f;

    /// <summary>
    /// Determines how much battery charge the entity's battery gains 
    /// per second when connected to an active APC power network.
    /// </summary>
    [DataField]
    public float BatteryRechargeRate { get; set; } = 50f;

    /// <summary>
    /// While the battery is being recharged, the load this entity places on the APC 
    /// power network is increased by the <see cref="BatteryRechargeRate"/> multiplied
    /// by this factor.
    /// </summary>
    [DataField]
    public float BatteryRechargeEfficiency { get; set; } = 1f;
}

/// <summary>
/// Raised whenever an ApcPowerReceiverBattery starts / stops discharging
/// </summary>
[ByRefEvent]
public readonly record struct ApcPowerReceiverBatteryChangedEvent(bool Enabled);
