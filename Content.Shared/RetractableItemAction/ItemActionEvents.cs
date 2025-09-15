using Content.Shared.Actions;

namespace Content.Shared.RetractableItemAction;

/// <summary>
/// Raised when using the RetractableItem action.
/// </summary>
[ByRefEvent]
public sealed partial class OnRetractableItemActionEvent : InstantActionEvent;
