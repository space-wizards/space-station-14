namespace Content.Shared.Actions.Events;

/// <summary>
/// Raised once an action is performed.
/// </summary>
[ByRefEvent]
public record struct ActionPerformedEvent(EntityUid User);
