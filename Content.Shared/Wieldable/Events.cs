namespace Content.Shared.Wieldable;

/// <summary>
/// Raised directed on an item when it is wielded.
/// </summary>
[ByRefEvent]
public readonly record struct ItemWieldedEvent(EntityUid User);

/// <summary>
/// Raised directed on an item that has been unwielded.
/// Force is whether the item is being forced to be unwielded, or if the player chose to unwield it themselves.
/// </summary>
[ByRefEvent]
public readonly record struct ItemUnwieldedEvent(EntityUid User, bool Force);

/// <summary>
/// Raised directed on an item before a user tries to wield it.
/// If this event is cancelled wielding will not happen.
/// </summary>
[ByRefEvent]
public record struct WieldAttemptEvent(EntityUid User, bool Cancelled = false)
{
    public void Cancel()
    {
        Cancelled = true;
    }
}

/// <summary>
/// Raised directed on an item before a user tries to stop wielding it willingly.
/// If this event is cancelled unwielding will not happen.
/// </summary>
/// <remarks>
/// This event is not raised if the user is forced to unwield the item.
/// </remarks>
[ByRefEvent]
public record struct UnwieldAttemptEvent(EntityUid User, bool Cancelled = false)
{
    public void Cancel()
    {
        Cancelled = true;
    }
}
