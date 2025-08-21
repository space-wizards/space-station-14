namespace Content.Shared.Actions.Events;

/// <summary>
/// Raised on an action entity to have the event-holding component cast and set its event.
/// If it was set successfully then <c>Handled</c> must be set to true.
/// </summary>
[ByRefEvent]
public record struct ActionSetEventEvent(BaseActionEvent Event, bool Handled = false);
