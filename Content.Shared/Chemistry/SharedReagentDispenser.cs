using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Storage;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes; // Starlight-edit

namespace Content.Shared.Chemistry
{
    /// <summary>
    /// This class holds constants that are shared between client and server.
    /// </summary>
    public sealed class SharedReagentDispenser
    {
        public const string OutputSlotName = "beakerSlot";
    }

    [Serializable, NetSerializable]
    public sealed class ReagentDispenserSetDispenseAmountMessage : BoundUserInterfaceMessage
    {
        public readonly ReagentDispenserDispenseAmount ReagentDispenserDispenseAmount;

        public ReagentDispenserSetDispenseAmountMessage(ReagentDispenserDispenseAmount amount)
        {
            ReagentDispenserDispenseAmount = amount;
        }

        /// <summary>
        ///     Create a new instance from interpreting a String as an integer,
        ///     throwing an exception if it is unable to parse.
        /// </summary>
        public ReagentDispenserSetDispenseAmountMessage(String s)
        {
            switch (s)
            {
                case "1":
                    ReagentDispenserDispenseAmount = ReagentDispenserDispenseAmount.U1;
                    break;
                case "5":
                    ReagentDispenserDispenseAmount = ReagentDispenserDispenseAmount.U5;
                    break;
                case "10":
                    ReagentDispenserDispenseAmount = ReagentDispenserDispenseAmount.U10;
                    break;
                case "15":
                    ReagentDispenserDispenseAmount = ReagentDispenserDispenseAmount.U15;
                    break;
                case "20":
                    ReagentDispenserDispenseAmount = ReagentDispenserDispenseAmount.U20;
                    break;
                case "25":
                    ReagentDispenserDispenseAmount = ReagentDispenserDispenseAmount.U25;
                    break;
                case "30":
                    ReagentDispenserDispenseAmount = ReagentDispenserDispenseAmount.U30;
                    break;
                case "50":
                    ReagentDispenserDispenseAmount = ReagentDispenserDispenseAmount.U50;
                    break;
                case "100":
                    ReagentDispenserDispenseAmount = ReagentDispenserDispenseAmount.U100;
                    break;
                default:
                    throw new Exception($"Cannot convert the string `{s}` into a valid ReagentDispenser DispenseAmount");
            }
        }
    }

    [Serializable, NetSerializable]
    public sealed class ReagentDispenserDispenseReagentMessage : BoundUserInterfaceMessage
    {
        public readonly ReagentDispenseData Data; // Starlight-edit

        public ReagentDispenserDispenseReagentMessage(ReagentDispenseData data) // Starlight-edit
        {
            Data = data; // Starlight-edit
        }
    }

    /// <summary>
    ///     Message sent by the user interface to ask the reagent dispenser to eject a container
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ReagentDispenserEjectContainerMessage : BoundUserInterfaceMessage
    {
        public readonly ItemStorageLocation StorageLocation;

        public ReagentDispenserEjectContainerMessage(ItemStorageLocation storageLocation)
        {
            StorageLocation = storageLocation;
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
    public sealed class ReagentInventoryItem(ReagentDispenseData data, string reagentLabel, FixedPoint2 quantity, Color reagentColor, bool generatable) // Starlight-edit
    {
        public ReagentDispenseData Data = data; // Starlight-edit
        public string ReagentLabel = reagentLabel;
        public FixedPoint2 Quantity = quantity;
        public Color ReagentColor = reagentColor;
        public bool Generatable = generatable; // Starlight-edit
    }

    [Serializable, NetSerializable]
    public sealed class ReagentDispenserBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly ContainerInfo? OutputContainer;

        public readonly NetEntity? OutputContainerEntity;

        /// <summary>
        /// A list of the reagents which this dispenser can dispense.
        /// </summary>
        public readonly List<ReagentInventoryItem> Inventory;

        public readonly ReagentDispenserDispenseAmount SelectedDispenseAmount;

        public ReagentDispenserBoundUserInterfaceState(ContainerInfo? outputContainer, NetEntity? outputContainerEntity, List<ReagentInventoryItem> inventory, ReagentDispenserDispenseAmount selectedDispenseAmount)
        {
            OutputContainer = outputContainer;
            OutputContainerEntity = outputContainerEntity;
            Inventory = inventory;
            SelectedDispenseAmount = selectedDispenseAmount;
        }
    }
    
    // Starlight-start: Generatable reagents
    [Serializable, NetSerializable]
    public sealed class ReagentDispenseData(ItemStorageLocation? storageLocation, ProtoId<ReagentPrototype>? reagentID)
    {
        public ItemStorageLocation? StorageLocation = storageLocation;
        public ProtoId<ReagentPrototype>? ReagentID = reagentID;
    }
    // Starlight-end

    [Serializable, NetSerializable]
    public enum ReagentDispenserUiKey
    {
        Key
    }
}
