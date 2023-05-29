using Robust.Shared.Serialization;

namespace Content.Shared.Flesh;

[Serializable, NetSerializable]
public enum FleshHeartVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum FleshHeartStatus
{
    Active,
    Disable
}

[Serializable, NetSerializable]
public enum FleshHeartLayers : byte
{
    Base
}
