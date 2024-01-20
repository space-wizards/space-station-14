namespace Content.Shared.Climbing.Events;

/// <summary>
///     Raised on an entity when it successfully climbs on something.
/// </summary>
[ByRefEvent]
public readonly record struct StartClimbEvent(EntityUid Climbable);
