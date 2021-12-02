using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory.New.Events;

public class EquipAttemptEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// The entity equipping.
    /// </summary>
    public readonly EntityUid Equipee;

    /// <summary>
    /// The entity to be equipped.
    /// </summary>
    public readonly EntityUid Equipment;

    /// <summary>
    /// The slotFlags of the slot to equip the entity into.
    /// </summary>
    public readonly EquipmentSlotDefines.SlotFlags SlotFlags;

    /// <summary>
    /// If cancelling and wanting to provide a custom reason, use this field. Not that this expects a loc-id.
    /// </summary>
    public string? Reason;

    public EquipAttemptEvent(EntityUid equipee, EntityUid equipment, EquipmentSlotDefines.SlotFlags slotFlags)
    {
        Equipee = equipee;
        Equipment = equipment;
        SlotFlags = slotFlags;
    }
}
