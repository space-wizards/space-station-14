using Robust.Shared.Serialization;

namespace Content.Shared.Defusable;

/// <summary>
/// This handles defusable explosives, such as Syndicate Bombs.
/// </summary>
public abstract class SharedDefusableSystem : EntitySystem { }
// most of the logic is in the server

[NetSerializable, Serializable]
public enum DefusableVisuals
{
    Active,
    ActiveWires,
    Inactive,
    InactiveWires,
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
