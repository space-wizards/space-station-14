namespace Content.Shared.Actions.Events;

/// <summary>
/// Raised on an action entity to get its events.
/// </summary>
/// <param name="Events"></param>
[ByRefEvent]
public record struct ActionGetEventsEvent(List<BaseActionEvent>? Events = null);
