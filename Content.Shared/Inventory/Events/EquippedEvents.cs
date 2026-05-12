namespace Content.Shared.Inventory.Events;

public abstract class EquippedEventBase : EntityEventArgs
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
    /// The slot group the entity got equipped in.
    /// </summary>
    public readonly string SlotGroup;

    /// <summary>
    /// Slotflags of the slot the entity just got equipped to.
    /// </summary>
    public readonly SlotFlags SlotFlags;

    public EquippedEventBase(EntityUid equipee, EntityUid equipment, SlotDefinition slotDefinition)
    {
        Equipee = equipee;
        Equipment = equipment;
        Slot = slotDefinition.Name;
        SlotGroup = slotDefinition.SlotGroup;
        SlotFlags = slotDefinition.SlotFlags;
    }
}

/// <summary>
/// Raised directed on an equipee when something is equipped.
/// </summary>
public sealed class DidEquipEvent : EquippedEventBase
{
    public DidEquipEvent(EntityUid equipee, EntityUid equipment, SlotDefinition slotDefinition) : base(equipee, equipment, slotDefinition)
    {
    }
}

/// <summary>
/// Raised directed on equipment when it's equipped to an equipee
/// </summary>
public sealed class GotEquippedEvent : EquippedEventBase
{
    public GotEquippedEvent(EntityUid equipee, EntityUid equipment, SlotDefinition slotDefinition) : base(equipee, equipment, slotDefinition)
    {
    }
}
