using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory.Events;

public class UnequipAttemptEventBase : CancellableEntityEventArgs
{
    /// <summary>
    /// The entity unequipping.
    /// </summary>
    public readonly EntityUid Equipee;

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

    public UnequipAttemptEventBase(EntityUid equipee, EntityUid equipment, SlotDefinition slotDefinition)
    {
        Equipee = equipee;
        Equipment = equipment;
        Slot = slotDefinition.Name;
    }
}

public class BeingUnequippedAttemptEvent : UnequipAttemptEventBase
{
    public BeingUnequippedAttemptEvent(EntityUid equipee, EntityUid equipment, SlotDefinition slotDefinition) : base(equipee, equipment, slotDefinition)
    {
    }
}

public class IsUnequippingAttemptEvent : UnequipAttemptEventBase
{
    public IsUnequippingAttemptEvent(EntityUid equipee, EntityUid equipment, SlotDefinition slotDefinition) : base(equipee, equipment, slotDefinition)
    {
    }
}
