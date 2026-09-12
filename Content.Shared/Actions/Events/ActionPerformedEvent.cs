namespace Content.Shared.Actions.Events;

/// <summary>
///     Raised on the action entity when it is used and <see cref="BaseActionEvent.Handled"/>.
/// </summary>
/// <param name="Performer">The entity that performed this action.</param>
/// <param name="Target">The target of the action. If there is none, it is just the Performer.</param>
[ByRefEvent]
public readonly record struct ActionPerformedEvent(EntityUid Performer, EntityUid Target);
