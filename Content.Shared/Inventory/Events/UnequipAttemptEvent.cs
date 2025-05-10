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

/// <summary>
/// Raised on the item that is being unequipped.
/// </summary>
public sealed class BeingUnequippedAttemptEvent(EntityUid unequipee, EntityUid unEquipTarget, EntityUid equipment,
    SlotDefinition slotDefinition) : UnequipAttemptEventBase(unequipee, unEquipTarget, equipment, slotDefinition)
{
}

/// <summary>
/// Raised on the entity that is unequipping an item.
/// </summary>
public sealed class IsUnequippingAttemptEvent(EntityUid unequipee, EntityUid unEquipTarget, EntityUid equipment,
    SlotDefinition slotDefinition) : UnequipAttemptEventBase(unequipee, unEquipTarget, equipment, slotDefinition)
{
}

/// <summary>
/// Raised on the entity from who item is being unequipped.
/// </summary>
public sealed class IsUnequippingTargetAttemptEvent(EntityUid unequipee, EntityUid unEquipTarget, EntityUid equipment,
    SlotDefinition slotDefinition) : UnequipAttemptEventBase(unequipee, unEquipTarget, equipment, slotDefinition)
{
}
