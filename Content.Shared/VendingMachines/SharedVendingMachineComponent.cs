#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.DragDrop;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.VendingMachines
{
    public class SharedVendingMachineComponent : Component, IDragDropOn
    {
        public override string Name => "VendingMachine";
        public override uint? NetID => ContentNetIDs.VENDING_MACHINE;

        [ViewVariables]
        public List<VendingMachineInventoryEntry> Inventory = new();

        bool IDragDropOn.CanDragDropOn(DragDropEvent eventArgs)
        {
            if (!eventArgs.Dragged.HasComponent<SharedVendingMachineRestockComponent>())
            {
                return false;
            }

            // TODO: Once we get silicons need to check organic
            return true;
        }

        public virtual bool DragDropOn(DragDropEvent eventArgs)
        {
            return true;
        }

        [Serializable, NetSerializable]
        public enum VendingMachineVisuals
        {
            VisualState,
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
        public class VendingMachineEjectMessage : BoundUserInterfaceMessage
        {
            public readonly string ID;
            public VendingMachineEjectMessage(string id)
            {
                ID = id;
            }
        }

        [Serializable, NetSerializable]
        public enum VendingMachineUiKey
        {
            Key,
        }

        [Serializable, NetSerializable]
        public class InventorySyncRequestMessage : BoundUserInterfaceMessage
        {
        }

        [Serializable, NetSerializable]
        public class VendingMachineInventoryMessage : BoundUserInterfaceMessage
        {
            public readonly List<VendingMachineInventoryEntry> Inventory;
            public VendingMachineInventoryMessage(List<VendingMachineInventoryEntry> inventory)
            {
                Inventory = inventory;
            }
        }

        [Serializable, NetSerializable]
        public class VendingMachineInventoryEntry
        {
            [ViewVariables(VVAccess.ReadWrite)]
            public string ID;
            [ViewVariables(VVAccess.ReadWrite)]
            public uint Amount;
            public VendingMachineInventoryEntry(string id, uint amount)
            {
                ID = id;
                Amount = amount;
            }
        }
    }
}
