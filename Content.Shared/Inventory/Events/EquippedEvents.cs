using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory.Events;

public class EquippedEventBase : EntityEventArgs
{
    /// <summary>
    /// The entity equipping.
    /// </summary>
    public readonly EntityUid Equipee;

    /// <summary>
    /// The entity which got equipped.
    /// </summary>
    public readonly EntityUid Equipment;

    /// <summary>
    /// The slot the entity got equipped to.
    /// </summary>
    public readonly string Slot;

    /// <summary>
    /// Slotflags of the slot the entity just got equipped to.
    /// </summary>
    public readonly SlotFlags SlotFlags;

    public EquippedEventBase(EntityUid equipee, EntityUid equipment, SlotDefinition slotDefinition)
    {
        Equipee = equipee;
        Equipment = equipment;
        Slot = slotDefinition.Name;
        SlotFlags = slotDefinition.SlotFlags;
    }
}

public class DidEquipEvent : EquippedEventBase
{
    public DidEquipEvent(EntityUid equipee, EntityUid equipment, SlotDefinition slotDefinition) : base(equipee, equipment, slotDefinition)
    {
    }
}

public class GotEquippedEvent : EquippedEventBase
{
    public GotEquippedEvent(EntityUid equipee, EntityUid equipment, SlotDefinition slotDefinition) : base(equipee, equipment, slotDefinition)
    {
    }
}
