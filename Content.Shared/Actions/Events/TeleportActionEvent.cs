using Robust.Shared.Map;

namespace Content.Shared.Actions.Events;

[ByRefEvent]
public record struct TeleportActionEvent(EntityUid Performer, EntityCoordinates Target, bool Handled = false) { }

