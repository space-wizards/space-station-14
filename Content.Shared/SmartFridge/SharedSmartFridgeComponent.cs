using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Content.Shared.Chemistry.Components;

namespace Content.Shared.SmartFridge
{
    [NetworkedComponent()]
    public class SharedSmartFridgeComponent : Component
    {
        // [ViewVariables]
        public List<SmartFridgePublicListEntry> PublicInventory = new();


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
        public class SmartFridgeEjectMessage : BoundUserInterfaceMessage
        {
            public readonly string ID;
            public readonly bool EjectAll;
            public SmartFridgeEjectMessage(string id, bool ejectAll)
            {
                ID = id;
                EjectAll = ejectAll;
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
        public class SmartFridgeInventoryMessage : BoundUserInterfaceMessage
        {
            public readonly List<SmartFridgePublicListEntry> Inventory;
            public SmartFridgeInventoryMessage(List<SmartFridgePublicListEntry> inventory)
            {
                Inventory = inventory;
            }
        }

        [Serializable, NetSerializable]
        public class SmartFridgePublicListEntry
        {
            [ViewVariables(VVAccess.ReadWrite)]
            public int ID;
            [ViewVariables(VVAccess.ReadWrite)]
            public string Name;
            [ViewVariables(VVAccess.ReadWrite)]
            public uint Amount;
            public SmartFridgePublicListEntry(int id, string name, uint amount)
            {
                ID = id;
                Name = name;
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
    }
}
