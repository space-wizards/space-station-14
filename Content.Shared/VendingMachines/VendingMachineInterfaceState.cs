using Robust.Shared.Serialization;

namespace Content.Shared.VendingMachines
{
    [Serializable, NetSerializable]
    public sealed class VendingMachineEjectMessage : BoundUserInterfaceMessage
    {
        public readonly InventoryType Type;
        public readonly string ID;
        public VendingMachineEjectMessage(InventoryType type, string id)
        {
            Type = type;
            ID = id;
        }
    }
    
    // 🌟Starlight🌟 start
    /// <summary>
    /// Request balance information from server
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class VendingMachineRequestBalanceMessage : BoundUserInterfaceMessage
    {
    }

    /// <summary>
    /// Balance update from server to client
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class VendingMachineBalanceUpdateMessage : BoundUserInterfaceMessage
    {
        public readonly int Balance;
        public VendingMachineBalanceUpdateMessage(int balance)
        {
            Balance = balance;
        }
    }
    // 🌟Starlight🌟 end

    [Serializable, NetSerializable]
    public enum VendingMachineUiKey
    {
        Key,
    }
}
