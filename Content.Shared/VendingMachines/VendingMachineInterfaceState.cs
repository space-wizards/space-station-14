using Robust.Shared.Serialization;

namespace Content.Shared.VendingMachines
{
    [NetSerializable, Serializable]
    public sealed class VendingMachineInterfaceState : BoundUserInterfaceState
    {
        public List<VendingMachineInventoryEntry> Inventory;

        public VendingMachineInterfaceState(List<VendingMachineInventoryEntry> inventory)
        {
            Inventory = inventory;
        }
    }

    [Serializable, NetSerializable]
    public sealed class VendingMachineEjectMessage : BoundUserInterfaceMessage
    {
        public readonly string TypeId;
        public readonly string Id;

        public VendingMachineEjectMessage(string typeId, string id)
        {
            TypeId = typeId;
            Id = id;
        }
    }

    [Serializable, NetSerializable]
    public enum VendingMachineUiKey
    {
        Key,
    }
}
