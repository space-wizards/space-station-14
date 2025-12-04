using Robust.Shared.Serialization;

namespace Content.Shared.Defusable;

[NetSerializable, Serializable]
public enum DefusableVisuals
{
    Active
}

[NetSerializable, Serializable]
public enum DefusableWireStatus
{
    LiveIndicator,
    BoltIndicator,
    BoomIndicator,
    DelayIndicator,
    ProceedIndicator,
}
