using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory.Events;

public abstract class EquipAttemptBase : CancellableEntityEventArgs
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
    public readonly SlotFlags SlotFlags;

    /// <summary>
    /// If cancelling and wanting to provide a custom reason, use this field. Not that this expects a loc-id.
    /// </summary>
    public string? Reason;

    public EquipAttemptBase(EntityUid equipee, EntityUid equipment, SlotFlags slotFlags)
    {
        Equipee = equipee;
        Equipment = equipment;
        SlotFlags = slotFlags;
    }
}

public class BeingEquippedAttemptEvent : EquipAttemptBase
{
    public BeingEquippedAttemptEvent(EntityUid equipee, EntityUid equipment, SlotFlags slotFlags) : base(equipee, equipment, slotFlags)
    {
    }
}

public class IsEquippingAttemptEvent : EquipAttemptBase
{
    public IsEquippingAttemptEvent(EntityUid equipee, EntityUid equipment, SlotFlags slotFlags) : base(equipee, equipment, slotFlags)
    {
    }
}
