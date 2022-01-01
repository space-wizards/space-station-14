using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Inventory.Events;

[NetSerializable, Serializable]
public class TryEquipNetworkMessage : EntityEventArgs
{
    public readonly EntityUid Actor;
    public readonly EntityUid Target;
    public readonly EntityUid ItemUid;
    public readonly string Slot;
    public readonly bool Silent;
    public readonly bool Force;
    public readonly bool FromHands;

    public TryEquipNetworkMessage(EntityUid actor, EntityUid target, EntityUid itemUid, string slot, bool silent, bool force, bool fromHands)
    {
        Actor = actor;
        Target = target;
        ItemUid = itemUid;
        Slot = slot;
        Silent = silent;
        Force = force;
        FromHands = fromHands;
    }
}
