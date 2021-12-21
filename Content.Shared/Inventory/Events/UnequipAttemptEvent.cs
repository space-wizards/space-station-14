using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory.Events;

public class UnequipAttemptEventBase : CancellableEntityEventArgs
{
    /// <summary>
    /// The entity unequipping.
    /// </summary>
    public readonly EntityUid Unequipee;

    /// <summary>
    /// The entity being unequipped from.
    /// </summary>
    public readonly EntityUid UnEquipTarget;

    /// <summary>
    /// The entity to be unequipped.
    /// </summary>
    public readonly EntityUid Equipment;

    /// <summary>
    /// The slot the entity is being unequipped from.
    /// </summary>
    public readonly string Slot;

    /// <summary>
    /// If cancelling and wanting to provide a custom reason, use this field. Not that this expects a loc-id.
    /// </summary>
    public string? Reason;

    public UnequipAttemptEventBase(EntityUid unEquipTarget, EntityUid equipment, SlotDefinition slotDefinition, EntityUid unequipee)
    {
        UnEquipTarget = unEquipTarget;
        Equipment = equipment;
        Unequipee = unequipee;
        Slot = slotDefinition.Name;
    }
}

public class BeingUnequippedAttemptEvent : UnequipAttemptEventBase
{
    public BeingUnequippedAttemptEvent(EntityUid unEquipTarget, EntityUid equipment, SlotDefinition slotDefinition, EntityUid unequipee) : base(unEquipTarget, equipment, slotDefinition, unequipee)
    {
    }
}

public class IsUnequippingAttemptEvent : UnequipAttemptEventBase
{
    public IsUnequippingAttemptEvent(EntityUid unEquipTarget, EntityUid equipment, SlotDefinition slotDefinition, EntityUid unequipee) : base(unEquipTarget, equipment, slotDefinition, unequipee)
    {
    }
}
