namespace Content.Shared.Inventory.Events;

public abstract class UnequippedEventBase : EntityEventArgs
{
    /// <summary>
    /// The entity unequipping.
    /// </summary>
    public readonly EntityUid Equipee;

    /// <summary>
    /// The entity which got unequipped.
    /// </summary>
    public readonly EntityUid Equipment;

    /// <summary>
    /// The slot the entity got unequipped from.
    /// </summary>
    public readonly string Slot;

    /// <summary>
    /// The slot group the entity got unequipped from.
    /// </summary>
    public readonly string SlotGroup;

    public UnequippedEventBase(EntityUid equipee, EntityUid equipment, SlotDefinition slotDefinition)
    {
        Equipee = equipee;
        Equipment = equipment;
        Slot = slotDefinition.Name;
        SlotGroup = slotDefinition.SlotGroup;
    }
}

public sealed class DidUnequipEvent : UnequippedEventBase
{
    public DidUnequipEvent(EntityUid equipee, EntityUid equipment, SlotDefinition slotDefinition) : base(equipee, equipment, slotDefinition)
    {
    }
}

public sealed class GotUnequippedEvent : UnequippedEventBase
{
    public GotUnequippedEvent(EntityUid equipee, EntityUid equipment, SlotDefinition slotDefinition) : base(equipee, equipment, slotDefinition)
    {
    }
}
