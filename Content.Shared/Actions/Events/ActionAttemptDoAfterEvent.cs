using Content.Shared.Actions.Components;

namespace Content.Shared.Actions.Events;

/// <summary>
/// Raised when attempting to run a DoAfter on an Action, used to trigger a DoAfter on an Action (if it has the DoAfter component)
/// </summary>
/// <param name="Performer">The action performer</param>
/// <param name="OriginalUseDelay">The original action use delay, used for repeating actions</param>
/// <param name="Input">The original request, for validating</param>
[ByRefEvent]
public record struct ActionAttemptDoAfterEvent(Entity<ActionsComponent?> Performer, TimeSpan? OriginalUseDelay, RequestPerformActionEvent Input);
