namespace Content.Shared.Actions.Events;

/// <summary>
/// Raised on an action entity to get its event.
/// </summary>
[ByRefEvent]
public record struct ActionGetEventEvent(BaseActionEvent? Event = null);
