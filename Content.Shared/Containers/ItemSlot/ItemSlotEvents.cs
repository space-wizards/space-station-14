using Robust.Shared.Serialization;

namespace Content.Shared.Containers.ItemSlots;

/// <summary>
/// Used for item-slot insert and eject buttons.
/// </summary>
[Serializable, NetSerializable]
public sealed class ItemSlotButtonPressedEvent : BoundUserInterfaceMessage
{
    /// <summary>
    /// The ID of the slot/container from which to insert or eject an item.
    /// </summary>
    public string SlotId;

    /// <summary>
    /// Whether to attempt to insert an item into the slot if there is not already one inside.
    /// </summary>
    public bool TryInsert;

    /// <summary>
    /// Whether to attempt to eject the item from the slot if it has one.
    /// </summary>
    public bool TryEject;

    public ItemSlotButtonPressedEvent(string slotId, bool tryEject = true, bool tryInsert = true)
    {
        SlotId = slotId;
        TryEject = tryEject;
        TryInsert = tryInsert;
    }
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
