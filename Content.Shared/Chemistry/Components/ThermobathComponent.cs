using Content.Shared.Atmos;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Marks a device that heats or cools solutions in an inserted container.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ThermobathSystem))]
[AutoGenerateComponentState(true, fieldDeltas: true)]
public sealed partial class ThermobathComponent : Component
{
    public const string BeakerSlotId = "beakerSlot";

    [DataField, AutoNetworkedField]
    public ThermobathMode Mode = ThermobathMode.Auto;

    /// <summary>
    /// Target temperature in kelvin.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Setpoint = Atmospherics.T20C;

    /// <summary>
    /// Maximum allowed target temperature.
    /// </summary>
    [DataField]
    public float MaxTemperature = 573.15f; // 300 °C, taken from HUBER CC-308B datasheet

    /// <summary>
    /// Minimum allowed target temperature.
    /// </summary>
    [DataField]
    public float MinTemperature = 253.15f; // -20 °C, taken from HUBER CC-308B datasheet

    /// <summary>
    /// Heating power in watts.
    /// </summary>
    [DataField]
    public float HeatingPower = 200f;

    /// <summary>
    /// Cooling power in watts.
    /// </summary>
    [DataField]
    public float CoolingPower = 60f;

    /// <summary>
    /// Minimum energy change per regulator update in joules.
    /// </summary>
    public float MinEnergy;

    /// <summary>
    /// Maximum energy change per regulator update in joules.
    /// </summary>
    public float MaxEnergy;
}

[Serializable, NetSerializable]
public sealed class ThermobathPowerChangedMessage(bool enabled) : BoundUserInterfaceMessage
{
    public readonly bool Enabled = enabled;
}

[Serializable, NetSerializable]
public sealed class ThermobathSetpointChangedMessage(float setpoint) : BoundUserInterfaceMessage
{
    public readonly float Setpoint = setpoint;
}

[Serializable, NetSerializable]
public sealed class ThermobathModeChangedMessage(ThermobathMode mode) : BoundUserInterfaceMessage
{
    public readonly ThermobathMode Mode = mode;
}

[Serializable, NetSerializable]
public enum ThermobathMode : byte
{
    Cooling = 0,
    Auto = 1,
    Heating = 2
}

[Serializable, NetSerializable]
public enum ThermobathUiKey : byte
{
    Key
}
