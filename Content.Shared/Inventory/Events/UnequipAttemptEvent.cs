namespace Content.Shared.Inventory.Events;

public abstract class UnequipAttemptEventBase : CancellableEntityEventArgs
{
    /// <summary>
    /// The entity performing the action. NOT necessarily the same as the entity whose equipment is being removed..
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
    /// The slotFlags of the slot this item is being removed from.
    /// </summary>
    public readonly SlotFlags SlotFlags;

    /// <summary>
    /// The slot the entity is being unequipped from.
    /// </summary>
    public readonly string Slot;

    /// <summary>
    /// If cancelling and wanting to provide a custom reason, use this field. Not that this expects a loc-id.
    /// </summary>
    public string? Reason;

    public UnequipAttemptEventBase(EntityUid unequipee, EntityUid unEquipTarget, EntityUid equipment,
        SlotDefinition slotDefinition)
    {
        UnEquipTarget = unEquipTarget;
        Equipment = equipment;
        Unequipee = unequipee;
        SlotFlags = slotDefinition.SlotFlags;
        Slot = slotDefinition.Name;
    }
}

public sealed class BeingUnequippedAttemptEvent : UnequipAttemptEventBase
{
    public BeingUnequippedAttemptEvent(EntityUid unequipee, EntityUid unEquipTarget, EntityUid equipment,
        SlotDefinition slotDefinition) : base(unequipee, unEquipTarget, equipment, slotDefinition)
    {
    }
}

public sealed class IsUnequippingAttemptEvent : UnequipAttemptEventBase
{
    public IsUnequippingAttemptEvent(EntityUid unequipee, EntityUid unEquipTarget, EntityUid equipment,
        SlotDefinition slotDefinition) : base(unequipee, unEquipTarget, equipment, slotDefinition)
    {
    }
}
