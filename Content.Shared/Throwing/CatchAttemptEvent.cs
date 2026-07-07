namespace Content.Shared.Throwing;

/// <summary>
/// Raised on someone when they try to catch an item.
/// </summary>
[ByRefEvent]
public record struct CatchAttemptEvent(EntityUid Item, float CatchChance, bool Cancelled = false);
