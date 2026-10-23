namespace Content.Shared.Storage.Events;

/// <summary>
/// Raised on a bag deposit target to insert one item. Set <see cref="Handled"/> only if it was accepted.
/// </summary>
[ByRefEvent]
public record struct BagDepositAttemptEvent(EntityUid User, EntityUid Item, bool Handled = false);
