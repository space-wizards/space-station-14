using Robust.Shared.Serialization;

namespace Content.Shared.Pointing;

// TODO just make pointing properly predicted?
/// <summary>
///     Event raised when someone runs the client-side pointing verb.
/// </summary>
[Serializable, NetSerializable]
public sealed class PointingAttemptEvent : EntityEventArgs
{
    public EntityUid Target;

    public PointingAttemptEvent(EntityUid target)
    {
        Target = target;
    }
}
