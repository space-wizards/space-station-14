namespace Content.Shared.Actions.Events;

/// <summary>
/// Raised when attempting to run a DoAfter on an Action, used to trigger a DoAfter on an Action (if it has the DoAfter component)
/// </summary>
[ByRefEvent]
public record struct ActionAttemptDoAfterEvent(EntityUid User, RequestPerformActionEvent requestEvent);
