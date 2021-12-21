using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory.Events;

public abstract class EquipAttemptBase : CancellableEntityEventArgs
{
    /// <summary>
    /// The entity unequipping.
    /// </summary>
    public readonly EntityUid Equipee;

    /// <summary>
    /// The entity being unequipped from.
    /// </summary>
    public readonly EntityUid EquipTarget;

    /// <summary>
    /// The entity to be equipped.
    /// </summary>
    public readonly EntityUid Equipment;

    /// <summary>
    /// The slotFlags of the slot to equip the entity into.
    /// </summary>
    public readonly SlotFlags SlotFlags;

    /// <summary>
    /// The slot the entity is being equipped to.
    /// </summary>
    public readonly string Slot;

    /// <summary>
    /// If cancelling and wanting to provide a custom reason, use this field. Not that this expects a loc-id.
    /// </summary>
    public string? Reason;

    public EquipAttemptBase(EntityUid equipTarget, EntityUid equipment, SlotDefinition slotDefinition, EntityUid equipee)
    {
        EquipTarget = equipTarget;
        Equipment = equipment;
        Equipee = equipee;
        SlotFlags = slotDefinition.SlotFlags;
        Slot = slotDefinition.Name;
    }
}

public class BeingEquippedAttemptEvent : EquipAttemptBase
{
    public BeingEquippedAttemptEvent(EntityUid equipTarget, EntityUid equipment, SlotDefinition slotDefinition, EntityUid equipee) : base(equipTarget, equipment, slotDefinition, equipee)
    {
    }
}

public class IsEquippingAttemptEvent : EquipAttemptBase
{
    public IsEquippingAttemptEvent(EntityUid equipTarget, EntityUid equipment, SlotDefinition slotDefinition, EntityUid equipee) : base(equipTarget, equipment, slotDefinition, equipee)
    {
    }
}
