using Robust.Shared.Map;

namespace Content.Shared.Actions.Events;

[ByRefEvent]
public record struct ValidateActionEntityWorldTargetEvent(
    EntityUid User,
    EntityUid? Target,
    EntityCoordinates? Coords,
    bool Cancelled = false);
