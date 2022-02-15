using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SmartFridge
{
    public class SharedSmartFridgeComponent : Component
    {
        [ViewVariables]
        public List<SmartFridgeInventoryEntry> Inventory = new();

        [Serializable, NetSerializable]
        public enum SmartFridgeVisuals
        {
            VisualState,
        }

        [Serializable, NetSerializable]
        public enum SmartFridgeVisualState
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
            public readonly uint ID;
            public readonly bool EjectAll;
            public SmartFridgeEjectMessage(uint id, bool ejectAll)
            {
                ID = id;
                EjectAll = ejectAll;
            }
        }

        [Serializable, NetSerializable]
        public enum SmartFridgeUiKey
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
            public readonly List<SmartFridgeInventoryEntry> Inventory;
            public SmartFridgeInventoryMessage(List<SmartFridgeInventoryEntry> inventory)
            {
                Inventory = inventory;
            }
        }

        [Serializable, NetSerializable]
        public class SmartFridgeInventoryEntry
        {
            [ViewVariables(VVAccess.ReadWrite)]
            public uint ID;
            [ViewVariables(VVAccess.ReadWrite)]
            public string Name;
            [ViewVariables(VVAccess.ReadWrite)]
            public uint Amount;
            public SmartFridgeInventoryEntry(uint id, string name, uint amount)
            {
                ID = id;
                Name = name;
                Amount = amount;
            }
        }
    }
}
