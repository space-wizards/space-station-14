using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Containers.ItemSlots;

/// <summary>
/// Used for item-slot insert and eject buttons.
/// </summary>
[Serializable, NetSerializable]
public sealed class ItemSlotButtonPressedEvent(
    string slotId,
    bool tryEject = true,
    bool tryInsert = true) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The ID of the slot/container from which to insert or eject an item.
    /// </summary>
    public string SlotId = slotId;

    /// <summary>
    /// Whether to attempt to insert an item into the slot if there is not already one inside.
    /// </summary>
    public bool TryInsert = tryInsert;

    /// <summary>
    /// Whether to attempt to eject the item from the slot if it has one.
    /// </summary>
    public bool TryEject = tryEject;
}

/// <summary>
/// Do-after event used to finish ejecting an item from a specific item slot.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ItemSlotEjectDoAfterEvent : DoAfterEvent
{
    /// <summary>
    /// The ID of the slot the item is being ejected from.
    /// </summary>
    public string SlotId;

    public ItemSlotEjectDoAfterEvent(string slotId)
    {
        SlotId = slotId;
    }

    public override DoAfterEvent Clone() => this;
}

/// <summary>
/// Do-after event used to finish inserting an item into a specific item slot.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ItemSlotInsertDoAfterEvent : DoAfterEvent
{
    /// <summary>
    /// The ID of the slot the item is being inserted into.
    /// </summary>
    public string SlotId;

    /// <summary>
    /// Whether an existing item in the slot may be swapped out during insertion.
    /// </summary>
    public bool Swap;

    /// <summary>
    /// The item that was in the slot when insertion started, if any.
    /// </summary>
    public NetEntity? OriginalItem;

    public ItemSlotInsertDoAfterEvent(string slotId, bool swap, NetEntity? originalItem)
    {
        SlotId = slotId;
        Swap = swap;
        OriginalItem = originalItem;
    }

    public override DoAfterEvent Clone() => this;
}

/// <summary>
/// Event raised on the slot entity and the item being inserted to determine if insertion is allowed.
/// </summary>
[ByRefEvent]
public record struct ItemSlotInsertAttemptEvent(EntityUid SlotEntity, EntityUid Item, EntityUid? User, ItemSlot Slot, bool Cancelled = false);

/// <summary>
/// Event raised on the slot entity and the item being ejected to determine if ejection is allowed.
/// </summary>
[ByRefEvent]
public record struct ItemSlotEjectAttemptEvent(EntityUid SlotEntity, EntityUid Item, EntityUid? User, ItemSlot Slot, bool Cancelled = false);
