namespace Content.Shared.Actions.Events;

[ByRefEvent]
public record struct GetActionDataEvent(BaseActionComponent? Action);
