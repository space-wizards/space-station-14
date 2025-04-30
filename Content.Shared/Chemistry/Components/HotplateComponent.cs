using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Component for a laboratory hotplate device that can heat or cool solutions with SolutionHeater.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true), Access(typeof(SharedHotplateSystem))]
public sealed partial class HotplateComponent : Component
{
    /// <summary>
    /// Current temperature of the hotplate in Kelvin.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CurrentTemperature = 293.15f;

    /// <summary>
    /// Target temperature setpoint in Kelvin.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Setpoint = 293.15f;

    /// <summary>
    /// Maximum allowed temperature setpoint.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxTemperature = 573.15f; // 300C, taken from HUBER CC-308B datasheet

    /// <summary>
    /// Minimum allowed temperature setpoint.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinTemperature = 253.15f; // -20C, taken from HUBER CC-308B datasheet

    /// <summary>
    /// Operation mode of the hotplate.
    /// <seealso cref="HotplateMode"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public HotplateMode Mode = HotplateMode.Auto;

    /// <summary>
    /// Whether we currently have a beaker inserted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HasBeaker;

    /// <summary>
    /// Current active state of the hotplate.
    /// <seealso cref="HotplateActiveState"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public HotplateActiveState ActiveState = HotplateActiveState.Idle;

    /// <summary>
    /// Temperature hysteresis in Kelvin.
    /// </summary>
    [DataField]
    public float Hysteresis = 1.5f;

    /// <summary>
    /// Power scaling band beyond hysteresis.
    /// </summary>
    [DataField]
    public float ScaleBand = 6f;

    /// <summary>
    /// Heating power in watts.
    /// </summary>
    [DataField]
    public float HeatingPower = 100f;

    /// <summary>
    /// Cooling power in watts.
    /// </summary>
    [DataField]
    public float CoolingPower = 30f;
}

[Serializable, NetSerializable]
public sealed class HotplatePowerChangedMessage : BoundUserInterfaceMessage
{
    public bool Powered;

    public HotplatePowerChangedMessage(bool powered)
    {
        Powered = powered;
    }
}

[Serializable, NetSerializable]
public sealed class HotplateSetpointChangedMessage : BoundUserInterfaceMessage
{
    public float Setpoint;

    public HotplateSetpointChangedMessage(float setpoint)
    {
        Setpoint = setpoint;
    }
}

[Serializable, NetSerializable]
public sealed class HotplateModeChangedMessage : BoundUserInterfaceMessage
{
    public HotplateMode Mode;

    public HotplateModeChangedMessage(HotplateMode mode)
    {
        Mode = mode;
    }
}

[Serializable, NetSerializable]
public enum HotplateUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum HotplateMode : byte
{
    Cooling = 0,
    Auto = 1,
    Heating = 2
}

[Serializable, NetSerializable]
public enum HotplateActiveState : byte
{
    Idle = 0,
    Cooling = 1,
    Heating = 2
}
