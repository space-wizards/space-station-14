namespace Content.Shared.Bed.Sleep;

/// <summary>
///     Raised by an entity about to fall asleep.
///     Set Cancelled to true on event handling to interrupt
/// </summary>
[ByRefEvent]
public record struct TryingToSleepEvent(EntityUid uid, bool Cancelled = false);
