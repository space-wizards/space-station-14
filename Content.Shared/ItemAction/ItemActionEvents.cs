using Content.Shared.Actions;

namespace Content.Shared.ItemAction;

/// <summary>
/// Raised when using the ItemRecall action.
/// </summary>
[ByRefEvent]
public sealed partial class OnItemActionEvent : InstantActionEvent;
