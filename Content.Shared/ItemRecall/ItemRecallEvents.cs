using Content.Shared.Actions;

namespace Content.Shared.ItemRecall;

/// <summary>
/// Raise on using the ItemRecall action.
/// </summary>
[ByRefEvent]
public sealed partial class OnItemRecallActionEvent : InstantActionEvent;

/// <summary>
/// Raised on the item to recall it back to its user.
/// </summary>
[ByRefEvent]
public record struct RecallItemEvent;
