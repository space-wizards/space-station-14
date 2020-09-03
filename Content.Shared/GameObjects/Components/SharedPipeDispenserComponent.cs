using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.GameObjects.Components
{
    public class SharedPipeDispenserComponent : Component
    {
        public override string Name => "PipeDispenser";
        public override uint? NetID => ContentNetIDs.PIPE_DISPENSER;

        public List<PipeDispenserInventoryEntry> Inventory = new List<PipeDispenserInventoryEntry>();

        [Serializable, NetSerializable]
        public enum PipeDispenserVisuals
        {
            VisualState,
        }

        [Serializable, NetSerializable]
        public enum PipeDispenserVisualState
        {
            Normal,
            Off,
            Broken,
            Eject
        }

        [Serializable, NetSerializable]
        public class PipeDispenserEjectMessage : BoundUserInterfaceMessage
        {
            public readonly string ID;
            public readonly uint Amount;
            public PipeDispenserEjectMessage(string id, uint amount)
            {
                ID = id;
                Amount = amount;
            }
        }

        [Serializable, NetSerializable]
        public enum PipeDispenserUiKey
        {
            Key,
        }

        [Serializable, NetSerializable]
        public class InventorySyncRequestMessage : BoundUserInterfaceMessage
        {
        }

        [Serializable, NetSerializable]
        public class PipeDispenserInventoryMessage : BoundUserInterfaceMessage
        {
            public readonly List<PipeDispenserInventoryEntry> Inventory;
            public PipeDispenserInventoryMessage(List<PipeDispenserInventoryEntry> inventory)
            {
                Inventory = inventory;
            }
        }

        [Serializable, NetSerializable]
        public class PipeDispenserInventoryEntry
        {
            public string ID;
            public PipeDispenserInventoryEntry(string id)
            {
                ID = id;
            }
        }
    }
}
