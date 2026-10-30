using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Trinary.Components;

[Serializable, NetSerializable]
public enum GasMixerUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class GasMixerToggleStatusMessage : BoundUserInterfaceMessage
{
    public bool Enabled { get; }

    public GasMixerToggleStatusMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class GasMixerChangeOutputPressureMessage : BoundUserInterfaceMessage
{
    public float Pressure { get; }

    public GasMixerChangeOutputPressureMessage(float pressure)
    {
        Pressure = pressure;
    }
}

[Serializable, NetSerializable]
public sealed class GasMixerChangeNodePercentageMessage : BoundUserInterfaceMessage
{
    public float NodeOne { get; }

    public GasMixerChangeNodePercentageMessage(float nodeOne)
    {
        NodeOne = nodeOne;
    }
}
