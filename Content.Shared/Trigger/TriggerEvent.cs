namespace Content.Shared.Trigger;

/// <summary>
/// Raised whenever something is Triggered on the entity.
/// </summary>
/// <param name="User">The entity that activated the trigger.</param>
/// <param name="Key">
/// Allows to have multiple independent triggers on the same entity.
/// Setting this to null will activate all triggers.
/// </param>
/// <param name="Handled">Marks the event as handled if at least one trigger effect was activated.</param>
[ByRefEvent]
public record struct TriggerEvent(EntityUid? User = null, string? Key = null, bool Handled = false);

/// <summary>
/// Raised before a trigger is activated.
/// Cancelling prevents it from triggering.
/// </summary>
/// <param name="User">The entity that activated the trigger.</param>
/// <param name="CancelKeys">A set of keys from conditions to trigger if the attempt is cancelled.</param>
/// <param name="Key">
/// Allows to have multiple independent triggers on the same entity.
/// Setting this to null will activate all triggers.
/// </param>
/// <param name="Cancelled">Marks if the attempt has failed and to not go through with the trigger.</param>
[ByRefEvent]
public record struct AttemptTriggerEvent(EntityUid? User, HashSet<string> CancelKeys, string? Key = null, bool Cancelled = false);

/// <summary>
/// Raised when a timer trigger becomes active.
/// </summary>
/// <param name="User">The entity that activated the trigger.</param>
[ByRefEvent]
public readonly record struct ActiveTimerTriggerEvent(EntityUid? User);
