namespace Content.Shared.Repairable.Events;

/// <summary>
/// Raised when repair is attempted to check if it should be allowed.
/// </summary>
[ByRefEvent]
public record struct RepairAttemptEvent(EntityUid User, bool Cancelled = false);
