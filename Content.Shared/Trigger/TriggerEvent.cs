namespace Content.Shared.Trigger;

/// <summary>
/// Raised whenever something is Triggered on the entity.
/// </summary>
[ByRefEvent]
public record struct TriggerEvent(EntityUid Triggered, EntityUid? User = null, string? Key = null, bool Handled = false);

/// <summary>
/// Raised before a trigger is activated.
/// Cancelling prevents it from triggering.
/// </summary>
[ByRefEvent]
public record struct AttemptTriggerEvent(EntityUid Triggered, EntityUid? User, string? Key = null, bool Cancelled = false);

/// <summary>
/// Raised when timer trigger becomes active.
/// </summary>
[ByRefEvent]
public readonly record struct ActiveTimerTriggerEvent(EntityUid Triggered, EntityUid? User);
