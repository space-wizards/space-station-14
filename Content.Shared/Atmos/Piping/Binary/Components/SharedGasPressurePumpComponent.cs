using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Binary.Components;

[Serializable, NetSerializable]
public enum GasPressurePumpUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed partial class GasPressurePumpToggleStatusMessage(bool enabled) : BoundUserInterfaceMessage
{
    public bool Enabled { get; } = enabled;
}

[Serializable, NetSerializable]
public sealed partial class GasPressurePumpChangeOutputPressureMessage(float pressure) : BoundUserInterfaceMessage
{
    public float Pressure { get; } = pressure;
}

