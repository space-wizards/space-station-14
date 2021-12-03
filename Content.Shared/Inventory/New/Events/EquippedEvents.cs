using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory.New.Events;

public class EquippedEventBase : EntityEventArgs
{
    /// <summary>
    /// The entity equipping.
    /// </summary>
    public readonly EntityUid Equipee;

    /// <summary>
    /// The entity to be equipped.
    /// </summary>
    public readonly EntityUid Equipment;

    public EquippedEventBase(EntityUid equipee, EntityUid equipment)
    {
        Equipee = equipee;
        Equipment = equipment;
    }
}

public class DidEquipEvent : EquippedEventBase
{
    public DidEquipEvent(EntityUid equipee, EntityUid equipment) : base(equipee, equipment)
    {
    }
}

public class GotEquippedEvent : EquippedEventBase
{
    public GotEquippedEvent(EntityUid equipee, EntityUid equipment) : base(equipee, equipment)
    {
    }
}
