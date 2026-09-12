using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Temperature.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Marks a device that heats or cools solutions in an inserted container.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedThermobathSystem))]
public sealed partial class ThermobathComponent : Component
{
    public const string BeakerSlotId = "beakerSlot";
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
public sealed class ThermobathModeChangedMessage(ThermoregulatorMode mode) : BoundUserInterfaceMessage
{
    public readonly ThermoregulatorMode Mode = mode;
}

[Serializable, NetSerializable]
public enum ThermobathUiKey : byte
{
    Key
}
