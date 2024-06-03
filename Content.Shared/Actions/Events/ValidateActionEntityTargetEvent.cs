namespace Content.Shared.Actions.Events;

[ByRefEvent]
public record struct ValidateActionEntityTargetEvent(EntityUid User, EntityUid Target, bool Cancelled = false);
