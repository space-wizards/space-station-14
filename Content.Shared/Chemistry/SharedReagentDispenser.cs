using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    /// <summary>
    /// This class holds constants that are shared between client and server.
    /// </summary>
    public sealed class SharedReagentDispenser
    {
        public const string OutputSlotName = "ReagentDispenser-beakerSlot";
    }

    [Serializable, NetSerializable]
    public sealed class ReagentDispenserSetDispenseAmountMessage : BoundUserInterfaceMessage
    {
        public readonly ReagentDispenserDispenseAmount ReagentDispenserDispenseAmount;

        public ReagentDispenserSetDispenseAmountMessage(ReagentDispenserDispenseAmount amount)
        {
            ReagentDispenserDispenseAmount = amount;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ReagentDispenserDispenseReagentMessage : BoundUserInterfaceMessage
    {
        public readonly string SlotId;

        public ReagentDispenserDispenseReagentMessage(string slotId)
        {
            SlotId = slotId;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ReagentDispenserClearContainerSolutionMessage : BoundUserInterfaceMessage
    {

    }

    public enum ReagentDispenserDispenseAmount
    {
        U1 = 1,
        U5 = 5,
        U10 = 10,
        U15 = 15,
        U20 = 20,
        U25 = 25,
        U30 = 30,
        U50 = 50,
        U100 = 100,
    }

    [Serializable, NetSerializable]
    public sealed class ReagentDispenserBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly ContainerInfo? OutputContainer;
        /// <summary>
        /// A list of the reagents which this dispenser can dispense.
        /// </summary>
        public readonly List<KeyValuePair<string, KeyValuePair<string, string>>> Inventory;

        public readonly ReagentDispenserDispenseAmount SelectedDispenseAmount;

        public ReagentDispenserBoundUserInterfaceState(ContainerInfo? outputContainer, List<KeyValuePair<string, KeyValuePair<string, string>>> inventory, ReagentDispenserDispenseAmount selectedDispenseAmount)
        {
            OutputContainer = outputContainer;
            Inventory = inventory;
            SelectedDispenseAmount = selectedDispenseAmount;
        }
    }

    [Serializable, NetSerializable]
    public enum ReagentDispenserUiKey
    {
        Key
    }
}
