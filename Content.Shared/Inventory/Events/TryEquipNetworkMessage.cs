using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory.Events;

public class TryEquipNetworkMessage : EntityEventArgs
{
    public readonly EntityUid Uid;
    public readonly EntityUid ItemUid;
    public readonly string Slot;
    public readonly bool Silent;
    public readonly bool Force;

    public TryEquipNetworkMessage(EntityUid uid, EntityUid itemUid, string slot, bool silent, bool force)
    {
        Uid = uid;
        ItemUid = itemUid;
        Slot = slot;
        Silent = silent;
        Force = force;
    }
}
