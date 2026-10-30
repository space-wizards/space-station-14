namespace Content.Shared.Actions.Events;

/// <summary>
///     Raised on the action entity when it is used and <see cref="BaseActionEvent.Handled"/>.
/// </summary>
/// <param name="Performer">The entity that performed this action.</param>
/// <param name="Target">The target of the action, if any.</param>
[ByRefEvent]
public readonly record struct ActionPerformedEvent(EntityUid Performer, EntityUid? Target);
