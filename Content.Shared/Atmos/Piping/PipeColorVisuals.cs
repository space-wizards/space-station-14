using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping;

[Serializable, NetSerializable]
public enum PipeColorVisuals
{
    Color,
}

[Serializable, NetSerializable]
public enum PipeVisualLayers : byte
{
    Pipe,
    Connector,
    Device,
}
