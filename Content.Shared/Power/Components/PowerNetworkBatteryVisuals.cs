using Robust.Shared.Serialization;

namespace Content.Shared.Power.Components;

/// <summary>
/// AppearanceData for power network battery visuals
/// </summary>
[Serializable, NetSerializable]
public enum PowerNetworkBatteryVisuals
{
    /// <summary>
    /// The last charge level of the entity (int: value from 0 to N-1 depending on the visuals' max charge level count)
    /// </summary>
    LastChargeLevel,
    /// <summary>
    /// The last charge state of the entity (ChargeState: charging/discharging)
    /// </summary>
    LastChargeState,
    /// <summary>
    /// The last charge capabilities of the entity (PowerNetworkBatteryChargeCapabilities: can charge/discharge)
    /// </summary>
    LastChargeCapabilities
}

/// <summary>
/// Layers to be updated by PowerNetworkBatteryVisuals
/// </summary>
[Serializable, NetSerializable]
public enum PowerNetworkBatteryVisualLayers
{
    /// <summary>
    /// The layer showing the overall charge level of the device.
    /// </summary>
    ChargeLevel,
    /// <summary>
    /// The layer showing how the charge level is changing over time.
    /// </summary>
    ChargeState,
    /// <summary>
    /// The layer showing whether the device can charge or not.
    /// </summary>
    CanCharge,
    /// <summary>
    /// The layer showing whether the device can discharge or not.
    /// </summary>
    CanDischarge,
}

/// <summary>
/// Whether or not a given battery can charge/discharge.
/// </summary>
[Serializable, NetSerializable]
public enum PowerNetworkBatteryChargeCapabilities : byte
{
    /// <summary>
    /// PowerNetworkBatteryComponent.CanCharge == true
    /// </summary>
    CanCharge = 1 << 0,
    /// <summary>
    /// PowerNetworkBatteryComponent.CanDischarge == true
    /// </summary>
    CanDischarge = 1 << 1,
    /// <summary>
    /// A combination of CanCharge and CanDischarge.
    /// </summary>
    Both = CanCharge | CanDischarge,
    /// <summary>
    /// Neither CanCharge nor CanDischarge.
    /// </summary>
    Neither = 0,
}
