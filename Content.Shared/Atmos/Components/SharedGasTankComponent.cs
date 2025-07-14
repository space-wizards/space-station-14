using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Components;

[Serializable, NetSerializable]
public sealed class GasTankUpdateMessage(float pressure, bool internalsConnected) : BoundUserInterfaceMessage
{
    public float Pressure { get; } = pressure;
    public bool InternalsConnected { get; } = internalsConnected;
}

[Serializable, NetSerializable]
public enum SharedGasTankUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class GasTankToggleInternalsMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class GasTankSetPressureMessage(float pressure) : BoundUserInterfaceMessage
{
    public float Pressure = pressure;
}

[Serializable, NetSerializable]
public sealed class GasTankBoundUserInterfaceState : BoundUserInterfaceState
{
    public float TankPressure;
}
