using Robust.Shared.Physics.Components;

namespace Content.Shared.Movement.Pulling.Events;

/// <summary>
/// Raised directed on both puller and pullable.
/// </summary>
public sealed class PullStoppedMessage : PullMessage
{
    public PullStoppedMessage(EntityUid pullerUid, EntityUid pulledUid) : base(pullerUid, pulledUid)
    {
    }
}
