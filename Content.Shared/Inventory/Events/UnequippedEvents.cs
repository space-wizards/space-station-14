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

    public UnequippedEventBase(EntityUid equipee, EntityUid equipment)
    {
        Equipee = equipee;
        Equipment = equipment;
    }
}

public class DidUnequipEvent : UnequippedEventBase
{
    public DidUnequipEvent(EntityUid equipee, EntityUid equipment) : base(equipee, equipment)
    {
    }
}

public class GotUnequippedEvent : UnequippedEventBase
{
    public GotUnequippedEvent(EntityUid equipee, EntityUid equipment) : base(equipee, equipment)
    {
    }
}
