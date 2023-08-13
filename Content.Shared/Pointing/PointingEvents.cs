using Robust.Shared.Serialization;

namespace Content.Shared.Pointing;

// TODO just make pointing properly predicted?
// So true
/// <summary>
///     Event raised when someone runs the client-side pointing verb.
/// </summary>
[Serializable, NetSerializable]
public sealed class PointingAttemptEvent : EntityEventArgs
{
    public NetEntity Target;

    public PointingAttemptEvent(NetEntity target)
    {
        Target = target;
    }
}
