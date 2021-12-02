using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory.New.Events;

public class GotEquippedEvent : EntityEventArgs
{
    /// <summary>
    /// The entity equipping.
    /// </summary>
    public readonly EntityUid Equipee;

    /// <summary>
    /// The entity to be equipped.
    /// </summary>
    public readonly EntityUid Equipment;
    public GotEquippedEvent(EntityUid equipee, EntityUid equipment)
    {
        Equipee = equipee;
        Equipment = equipment;
    }
}
