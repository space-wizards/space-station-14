using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Trinary.Components;

[Serializable, NetSerializable]
public enum GasMixerUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class GasMixerToggleStatusMessage(bool enabled) : BoundUserInterfaceMessage
{
    public bool Enabled { get; } = enabled;
}

[Serializable, NetSerializable]
public sealed class GasMixerChangeOutputPressureMessage(float pressure) : BoundUserInterfaceMessage
{
    public float Pressure { get; } = pressure;
}

[Serializable, NetSerializable]
public sealed class GasMixerChangeNodePercentageMessage(float nodeOne) : BoundUserInterfaceMessage
{
    public float NodeOne { get; } = nodeOne;
}
