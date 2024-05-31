using Robust.Shared.Physics.Components;

namespace Content.Shared.Movement.Pulling.Events;

/// <summary>
/// Raised directed on puller and pullable to determine if it can be pulled.
/// </summary>
public sealed class PullAttemptEvent : PullMessage
{
    public PullAttemptEvent(EntityUid pullerUid, EntityUid pullableUid) : base(pullerUid, pullableUid) { }

    public bool Cancelled { get; set; }
}
