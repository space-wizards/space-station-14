using Robust.Shared.Map;

namespace Content.Shared.Actions.Events;

[ByRefEvent]
public record struct ValidateActionWorldTargetEvent(EntityUid User, EntityCoordinates Target, bool Cancelled = false);
