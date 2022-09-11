using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.VendingMachines
{
    [NetworkedComponent()]
    public abstract class SharedVendingMachineComponent : Component
    {
        /// <summary>
        /// PrototypeID for the vending machine's inventory, see <see cref="VendingMachineInventoryPrototype"/>
        /// </summary>
        [DataField("pack", customTypeSerializer: typeof(PrototypeIdSerializer<VendingMachineInventoryPrototype>))]
        public string PackPrototypeId = string.Empty;

        /// <summary>
        /// Used by the server to determine how long the vending machine stays in the "Deny" state.
        /// Used by the client to determine how long the deny animation should be played.
        /// </summary>
        [DataField("denyDelay")]
        public float DenyDelay = 2.0f;

        /// <summary>
        /// Used by the server to determine how long the vending machine stays in the "Eject" state.
        /// The selected item is dispensed afer this delay.
        /// Used by the client to determine how long the deny animation should be played.
        /// </summary>
        [DataField("ejectDelay")]
        public float EjectDelay = 1.2f;

        [ViewVariables]
        public Dictionary<string, VendingMachineInventoryEntry> Inventory = new();

        [ViewVariables]
        public Dictionary<string, VendingMachineInventoryEntry> EmaggedInventory = new();

        [ViewVariables]
        public Dictionary<string, VendingMachineInventoryEntry> ContrabandInventory = new();

        public bool Emagged;
        public bool Contraband;
    }

    [Serializable, NetSerializable]
    public sealed class VendingMachineInventoryEntry
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public InventoryType Type;
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
    public enum InventoryType : byte
    {
        Regular,
        Emagged,
        Contraband
    }

    [Serializable, NetSerializable]
    public enum VendingMachineVisuals
    {
        VisualState
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
