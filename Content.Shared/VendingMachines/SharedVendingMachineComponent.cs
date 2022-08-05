using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.VendingMachines
{
    [NetworkedComponent()]
    public abstract class SharedVendingMachineComponent : Component
    {
        [DataField("pack", customTypeSerializer: typeof(PrototypeIdSerializer<VendingMachineInventoryPrototype>))]
        public string PackPrototypeId = string.Empty;

        public TimeSpan AnimationDuration = TimeSpan.Zero;

        [ViewVariables] public List<VendingMachineInventoryEntry> Inventory = new();
        [ViewVariables] public List<VendingMachineInventoryEntry> EmaggedInventory = new();
        [ViewVariables] public List<VendingMachineInventoryEntry> ContrabandInventory = new();

        public List<VendingMachineInventoryEntry> AllInventory
        {
            get
            {
                var inventory = new List<VendingMachineInventoryEntry>(Inventory);

                if (Emagged) inventory.AddRange(EmaggedInventory);
                if (Contraband) inventory.AddRange(ContrabandInventory);

                return inventory;
            }
        }

        public bool Emagged;
        public bool Contraband;

        [Serializable, NetSerializable]
        public enum VendingMachineVisuals
        {
            VisualState,
            Inventory,
        }

        [Serializable, NetSerializable]
        public enum VendingMachineVisualState
        {
            Normal,
            Off,
            Broken,
            Eject,
            Deny,
        }

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

        [Serializable, NetSerializable]
        public enum VendingMachineUiKey
        {
            Key,
        }

        [Serializable, NetSerializable]
        public sealed class InventorySyncRequestMessage : BoundUserInterfaceMessage
        {
        }

        [Serializable, NetSerializable]
        public sealed class VendingMachineInventoryMessage : BoundUserInterfaceMessage
        {
            public readonly List<VendingMachineInventoryEntry> Inventory;
            public VendingMachineInventoryMessage(List<VendingMachineInventoryEntry> inventory)
            {
                Inventory = inventory;
            }
        }

        [Serializable, NetSerializable]
        public sealed class VendingMachineInventoryEntry
        {
            [ViewVariables(VVAccess.ReadWrite)] public InventoryType Type;
            [ViewVariables(VVAccess.ReadWrite)]
            public string ID;
            [ViewVariables(VVAccess.ReadWrite)]
            public uint Amount;
            public VendingMachineInventoryEntry(InventoryType type, string id, uint amount)
            {
                Type = type;
                ID = id;
                Amount = amount;
            }
        }
        [Serializable, NetSerializable]
        public enum VendingMachineWireStatus : byte
        {
            Power,
            Access,
            Advertisement,
            Limiter
        }

        [Serializable, NetSerializable]
        public enum InventoryType : byte
        {
            Regular,
            Emagged,
            Contraband
        }
    }

    [Serializable, NetSerializable]
    public enum ContrabandWireKey : byte
    {
        StatusKey,
        TimeoutKey
    }

    [Serializable, NetSerializable]
    public enum EjectWireKey : byte
    {
        StatusKey,
    }

    public sealed class VendingMachineSelfDispenseEvent : InstantActionEvent { };
}
