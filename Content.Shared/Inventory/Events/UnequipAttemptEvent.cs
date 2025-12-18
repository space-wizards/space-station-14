namespace Content.Shared.Inventory.Events;

public abstract class UnequipAttemptEventBase(EntityUid unequipee, EntityUid unEquipTarget, EntityUid equipment,
    SlotDefinition slotDefinition) : CancellableEntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;

    /// <summary>
    /// The entity performing the action. NOT necessarily the same as the entity whose equipment is being removed..
    /// </summary>
    public readonly EntityUid Unequipee = unequipee;

    /// <summary>
    /// The entity being unequipped from.
    /// </summary>
    public readonly EntityUid UnEquipTarget = unEquipTarget;

    /// <summary>
    /// The entity to be unequipped.
    /// </summary>
    public readonly EntityUid Equipment = equipment;

    /// <summary>
    /// The slotFlags of the slot this item is being removed from.
    /// </summary>
    public readonly SlotFlags SlotFlags = slotDefinition.SlotFlags;

    /// <summary>
    /// The slot the entity is being unequipped from.
    /// </summary>
    public readonly string Slot = slotDefinition.Name;

    /// <summary>
    /// If cancelling and wanting to provide a custom reason, use this field. Not that this expects a loc-id.
    /// </summary>
    public string? Reason;
}

/// <summary>
/// Raised on the item that is being unequipped.
/// </summary>
public sealed class BeingUnequippedAttemptEvent(EntityUid unequipee, EntityUid unEquipTarget, EntityUid equipment,
    SlotDefinition slotDefinition) : UnequipAttemptEventBase(unequipee, unEquipTarget, equipment, slotDefinition);

/// <summary>
/// Raised on the entity that is unequipping an item.
/// </summary>
public sealed class IsUnequippingAttemptEvent(EntityUid unequipee, EntityUid unEquipTarget, EntityUid equipment,
    SlotDefinition slotDefinition) : UnequipAttemptEventBase(unequipee, unEquipTarget, equipment, slotDefinition);

/// <summary>
/// Raised on the entity from who item is being unequipped.
/// </summary>
public sealed class IsUnequippingTargetAttemptEvent(EntityUid unequipee, EntityUid unEquipTarget, EntityUid equipment,
    SlotDefinition slotDefinition) : UnequipAttemptEventBase(unequipee, unEquipTarget, equipment, slotDefinition);
