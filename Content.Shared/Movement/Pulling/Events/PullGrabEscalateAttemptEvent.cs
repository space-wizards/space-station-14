namespace Content.Shared.Movement.Pulling.Events;

/// <summary>
/// Raised on the pulled entity when the puller tries to pull a target
/// they are already pulling. Subscribers can use this to escalate grabs.
/// </summary>
[ByRefEvent]
public record struct PullGrabEscalateAttemptEvent(EntityUid Puller, EntityUid Pulled);
