using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Temperature.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Component for a laboratory device that can heat or cool solutions.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true), Access(typeof(SharedThermobathSystem))]
public sealed partial class ThermobathComponent : Component
{
    /// <summary>
    /// Current temperature of the solution in Kelvin.
    /// </summary>
    // TODO: Take this straight from the solution maybe? Unsure how that works with networking right now.
    [DataField, AutoNetworkedField]
    public float? SolutionTemperature;

    /// <summary>
    /// Whether we currently have a beaker inserted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HasBeaker;
}

[Serializable, NetSerializable]
public sealed class ThermobathPowerChangedMessage : BoundUserInterfaceMessage
{
    public bool Powered;

    public ThermobathPowerChangedMessage(bool powered)
    {
        Powered = powered;
    }
}

[Serializable, NetSerializable]
public sealed class ThermobathSetpointChangedMessage : BoundUserInterfaceMessage
{
    public float Setpoint;

    public ThermobathSetpointChangedMessage(float setpoint)
    {
        Setpoint = setpoint;
    }
}

[Serializable, NetSerializable]
public sealed class ThermobathModeChangedMessage : BoundUserInterfaceMessage
{
    public ThermoregulatorMode Mode;

    public ThermobathModeChangedMessage(ThermoregulatorMode mode)
    {
        Mode = mode;
    }
}

[Serializable, NetSerializable]
public enum ThermobathUiKey : byte
{
    Key
}
