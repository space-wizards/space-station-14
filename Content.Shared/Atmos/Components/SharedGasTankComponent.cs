using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Components;

[Serializable, NetSerializable]
public sealed class GasTankUpdateMessage(float pressure, double airPressure, bool gasValve, bool internalsConnected) : BoundUserInterfaceMessage
{
    public float Pressure { get; } = pressure;
    public double AirPressure { get; } = airPressure;
    public bool GasValve { get; } = gasValve;
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
    public float Pressure;
    public double AirPressure;
    public bool GasValve;
    public bool InternalsConnected;
}

[Serializable, NetSerializable]
public sealed class GasTankToggleValveMessage: BoundUserInterfaceMessage;
