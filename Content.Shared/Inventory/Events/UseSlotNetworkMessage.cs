using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Inventory.Events;

[NetSerializable, Serializable]
public class UseSlotNetworkMessage : EntityEventArgs
{
    public readonly EntityUid Uid;
    public readonly string Slot;

    public UseSlotNetworkMessage(EntityUid uid, string slot)
    {
        Uid = uid;
        Slot = slot;
    }
}
