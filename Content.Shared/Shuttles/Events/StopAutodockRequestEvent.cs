using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Events;

/// <summary>
/// Raised on a client when it is no longer viewing a dock.
/// </summary>
[Serializable, NetSerializable]
public class StopAutodockRequestEvent : EntityEventArgs
{
    public EntityUid Entity;
}
