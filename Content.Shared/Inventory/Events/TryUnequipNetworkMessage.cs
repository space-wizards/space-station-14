using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Inventory.Events;

[NetSerializable, Serializable]
public class TryUnequipNetworkMessage : EntityEventArgs
{
    public readonly EntityUid Actor;
    public readonly EntityUid Target;
    public readonly string Slot;
    public readonly bool Silent;
    public readonly bool Force;

    public TryUnequipNetworkMessage(EntityUid actor, EntityUid target, string slot, bool silent, bool force)
    {
        Actor = actor;
        Target = target;
        Slot = slot;
        Silent = silent;
        Force = force;
    }
}
