using Content.Shared.PowerCell.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Power.Components;

/// <summary>
/// Marker component that makes an entity with <see cref="BatteryComponent"/> update its appearance data for use with visualizers.
/// Also works with an entity with <see cref="PowerCellSlotComponent"/> and will relay the state of the inserted powercell.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BatteryVisualsComponent : Component;

/// <summary>
/// Keys for the appearance data.
/// </summary>
[Serializable, NetSerializable]
public enum BatteryVisuals : byte
{
    /// <summary>
    /// The current charge state of the battery.
    /// Either full, empty, or neither.
    /// Uses a <see cref="BatteryState"/>.
    /// </summary>
    State,
    /// <summary>
    /// Is the battery currently charging or discharging?
    /// Uses a <see cref="BatteryChargingState"/>.
    /// </summary>
    Charging,
}

/// <summary>
/// Charge level status of the battery.
/// </summary>
[Serializable, NetSerializable]
public enum BatteryChargingState : byte
{
    /// <summary>
    /// BatteryComponent.ChargeRate &gt; 0
    /// </summary>
    Charging,
    /// <summary>
    /// BatteryComponent.ChargeRate &lt; 0
    /// </summary>
    Decharging,
    /// <summary>
    /// BatteryComponent.ChargeRate == 0
    /// </summary>
    Constant,
}
