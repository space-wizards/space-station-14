using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory.Events;

public class TryUnequipNetworkMessage : EntityEventArgs
{
    public readonly EntityUid Uid;
    public readonly string Slot;
    public readonly bool Silent;
    public readonly bool Force;

    public TryUnequipNetworkMessage(EntityUid uid, string slot, bool silent, bool force)
    {
        Uid = uid;
        Slot = slot;
        Silent = silent;
        Force = force;
    }
}
