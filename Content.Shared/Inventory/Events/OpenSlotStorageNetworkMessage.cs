using Robust.Shared.Serialization;

namespace Content.Shared.Inventory.Events;

[NetSerializable, Serializable]
public sealed class OpenSlotStorageNetworkMessage : EntityEventArgs
{
    public readonly string Slot;

    public OpenSlotStorageNetworkMessage(string slot)
    {
        Slot = slot;
    }
}
