using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Chemistry
{
    public class SharedReagentDispenserComponent : Component
    {
        public override string Name => "ReagentDispenser";

        public List<ReagentDispenserInventoryEntry> Inventory = new List<ReagentDispenserInventoryEntry>();

        [Serializable, NetSerializable]
        public class ReagentDispenserBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly bool HasBeaker;
            public readonly int BeakerCurrentVolume;
            public readonly int BeakerMaxVolume;
            public readonly string ContainerName;
            public readonly List<ReagentDispenserInventoryEntry> Inventory;
            public readonly List<Solution.ReagentQuantity> ContainerReagents;
            public readonly string DispenserName;

            public ReagentDispenserBoundUserInterfaceState(bool hasBeaker, int beakerCurrentVolume, int beakerMaxVolume, string containerName,
                List<ReagentDispenserInventoryEntry> inventory, string dispenserName, List<Solution.ReagentQuantity> containerReagents)
            {
                HasBeaker = hasBeaker;
                BeakerCurrentVolume = beakerCurrentVolume;
                BeakerMaxVolume = beakerMaxVolume;
                ContainerName = containerName;
                Inventory = inventory;
                DispenserName = dispenserName;
                ContainerReagents = containerReagents;
            }
        }

        [Serializable, NetSerializable]
        public class UiButtonPressedMessage : BoundUserInterfaceMessage
        {
            public readonly UiButton Button;
            public readonly int DispenseIndex; //Index of dispense button / reagent being pressed. Only used when a dispense button is pressed.

            public UiButtonPressedMessage(UiButton button, int dispenseIndex)
            {
                Button = button;
                DispenseIndex = dispenseIndex;
            }
        }

        [Serializable, NetSerializable]
        public enum ReagentDispenserUiKey
        {
            Key
        }

        public enum UiButton
        {
            Eject,
            Clear,
            SetDispenseAmount1,
            SetDispenseAmount5,
            SetDispenseAmount10,
            SetDispenseAmount25,
            SetDispenseAmount50,
            SetDispenseAmount100,
            Dispense
        }

        [Serializable, NetSerializable]
        public class ReagentDispenserInventoryEntry
        {
            public string ID;
            public ReagentDispenserInventoryEntry(string id)
            {
                ID = id;
            }
        }
    }
}
