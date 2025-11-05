namespace Content.Shared.Actions.Events;

[ByRefEvent]
public record struct ActionGetEventsEvent(List<BaseActionEvent>? Events = null);
