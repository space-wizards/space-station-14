using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory.Events;

public class UnequippedEventBase : EntityEventArgs
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

    public UnequippedEventBase(EntityUid equipee, EntityUid equipment, SlotDefinition slotDefinition)
    {
        Equipee = equipee;
        Equipment = equipment;
        Slot = slotDefinition.Name;
    }
}

public class DidUnequipEvent : UnequippedEventBase
{
    public DidUnequipEvent(EntityUid equipee, EntityUid equipment, SlotDefinition slotDefinition) : base(equipee, equipment, slotDefinition)
    {
    }
}

public class GotUnequippedEvent : UnequippedEventBase
{
    public GotUnequippedEvent(EntityUid equipee, EntityUid equipment, SlotDefinition slotDefinition) : base(equipee, equipment, slotDefinition)
    {
    }
}
