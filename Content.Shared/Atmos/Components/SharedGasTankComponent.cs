using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Components;

[Serializable, NetSerializable]
public enum SharedGasTankUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class GasTankToggleInternalsMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class GasTankSetPressureMessage : BoundUserInterfaceMessage
{
    public float Pressure;
}

[Serializable, NetSerializable]
public sealed class GasTankBoundUserInterfaceState : BoundUserInterfaceState
{
    public float TankPressure;
}
