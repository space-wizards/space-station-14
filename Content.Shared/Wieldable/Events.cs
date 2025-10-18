using Content.Shared.Inventory;

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
/// Raised directed on an user and all the items in their inventory and hands before they wield an item.
/// If this event is cancelled wielding will not happen.
/// </summary>
[ByRefEvent]
public record struct WieldAttemptEvent(EntityUid User, EntityUid Wielded, bool Cancelled = false) : IInventoryRelayEvent
{
    /// <summary>
    /// Popup message for the user to tell them why they cannot wield if Cancelled
    /// </summary>
    public string? Message;

    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.WITHOUT_POCKET;
    public void Cancel()
    {
        Cancelled = true;
    }
}

/// <summary>
/// Raised directed on an user and all the items in their inventory and hands before they unwield an item willingly.
/// If this event is cancelled unwielding will not happen.
/// </summary>
/// <remarks>
/// This event is not raised if the user is forced to unwield the item.
/// </remarks>
[ByRefEvent]
public record struct UnwieldAttemptEvent(EntityUid User, EntityUid Wielded, bool Cancelled = false) : IInventoryRelayEvent
{
    /// <summary>
    /// Popup message for the user to tell them why they cannot unwield if Cancelled
    /// </summary>
    public string? Message;

    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.WITHOUT_POCKET;
    public void Cancel()
    {
        Cancelled = true;
    }
}
