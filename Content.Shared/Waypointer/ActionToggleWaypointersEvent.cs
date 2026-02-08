using Content.Shared.Actions;

namespace Content.Shared.Waypointer;

/// <summary>
/// This is a simple action for when someone toggles their waypointers.
/// </summary>
[ByRefEvent]
public sealed partial class ActionToggleWaypointersEvent : InstantActionEvent;
