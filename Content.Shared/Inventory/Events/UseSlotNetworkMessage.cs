using Robust.Shared.Serialization;

namespace Content.Shared.Inventory.Events;

[NetSerializable, Serializable]
public sealed class UseSlotNetworkMessage : EntityEventArgs
{
    // The slot-owner is implicitly the client that is sending this message.
    // Otherwise clients could start forcefully undressing other clients.
    public readonly string Slot;

    public UseSlotNetworkMessage(string slot)
    {
        Slot = slot;
    }
}
