using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    /// <summary>
    /// This class holds constants that are shared between client and server.
    /// </summary>
    public sealed class SharedChemMaster
    {
        public const uint PillTypes = 20;
        public const string BufferSolutionName = "buffer";
        public const string InputSlotName = "beakerSlot";
        public const string OutputSlotName = "outputSlot";
        public const string PillSolutionName = "food";
        public const string BottleSolutionName = "drink";
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterSetModeMessage : BoundUserInterfaceMessage
    {
        public readonly ChemMasterMode ChemMasterMode;

        public ChemMasterSetModeMessage(ChemMasterMode mode)
        {
            ChemMasterMode = mode;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterSetPillTypeMessage : BoundUserInterfaceMessage
    {
        public readonly uint PillType;

        public ChemMasterSetPillTypeMessage(uint pillType)
        {
            PillType = pillType;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterReagentAmountButtonMessage : BoundUserInterfaceMessage
    {
        public readonly string ReagentId;
        public readonly ChemMasterReagentAmount Amount;
        public readonly bool FromBuffer;

        public ChemMasterReagentAmountButtonMessage(string reagentId, ChemMasterReagentAmount amount, bool fromBuffer)
        {
            ReagentId = reagentId;
            Amount = amount;
            FromBuffer = fromBuffer;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterCreatePillsMessage : BoundUserInterfaceMessage
    {
        public readonly uint Amount;
        public readonly string Label;

        public ChemMasterCreatePillsMessage(uint amount, string label)
        {
            Amount = amount;
            Label = label;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterCreateBottlesMessage : BoundUserInterfaceMessage
    {
        public readonly uint Amount;
        public readonly string Label;

        public ChemMasterCreateBottlesMessage(uint amount, string label)
        {
            Amount = amount;
            Label = label;
        }
    }

    public enum ChemMasterMode
    {
        Transfer,
        Discard,
    }

    public enum ChemMasterReagentAmount
    {
        U1 = 1,
        U5 = 5,
        U10 = 10,
        U25 = 25,
        All,
    }

    public static class ChemMasterReagentAmountToFixedPoint
    {
        public static FixedPoint2 GetFixedPoint(this ChemMasterReagentAmount amount)
        {
            if (amount == ChemMasterReagentAmount.All)
                return FixedPoint2.MaxValue;
            else
                return FixedPoint2.New((int)amount);
        }
    }

    /// <summary>
    /// Information about a single line item in a container.
    /// This is intended to be generic over reagents and entities. See usage in <see cref="ContainerInfo"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public struct LineItemInfo
    {
        public readonly string Id;
        public readonly bool IsReagent;
        public readonly FixedPoint2 Quantity;

        public LineItemInfo(string id, bool isReagent, FixedPoint2 quantity)
        {
            Id = id;
            IsReagent = isReagent;
            Quantity = quantity;
        }
    }
    
    /// <summary>
    /// Information about the capacity and contents of a container for display in the UI
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ContainerInfo
    {
        /// <summary>
        /// The currently used volume of the container
        /// </summary>
        public readonly FixedPoint2 CurrentVolume;
        /// <summary>
        /// The maximum volume of the container
        /// </summary>
        public readonly FixedPoint2 MaxVolume;
        /// <summary>
        /// The container name to show to the player
        /// </summary>
        public readonly string DisplayName;
        /// <summary>
        /// A list of the reagents/items and their amounts within the container
        /// </summary>
        // todo: this causes NetSerializer exceptions if it's an IReadOnlyList (which would be preferred)
        public readonly List<LineItemInfo> Contents;

        public ContainerInfo(
            string displayName,
            FixedPoint2 currentVolume,
            FixedPoint2 maxVolume,
            List<LineItemInfo> contents)
        {
            DisplayName = displayName;
            CurrentVolume = currentVolume;
            MaxVolume = maxVolume;
            Contents = contents;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChemMasterBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly ContainerInfo? InputContainerInfo;
        public readonly ContainerInfo? OutputContainerInfo;
        
        /// <summary>
        /// A list of the reagents and their amounts within the buffer, if applicable.
        /// </summary>
        public readonly IReadOnlyList<Solution.ReagentQuantity> BufferReagents;
        public readonly string DispenserName;

        public readonly ChemMasterMode Mode;

        public readonly FixedPoint2? BufferCurrentVolume;
        public readonly uint SelectedPillType;

        public readonly uint PillProductionLimit;
        public readonly uint BottleProductionLimit;

        public readonly bool UpdateLabel;

        public ChemMasterBoundUserInterfaceState(
            ChemMasterMode mode, string dispenserName,
            ContainerInfo? inputContainerInfo, ContainerInfo? outputContainerInfo,
            IReadOnlyList<Solution.ReagentQuantity> bufferReagents, FixedPoint2 bufferCurrentVolume,
            uint selectedPillType, uint pillProductionLimit, uint bottleProductionLimit, bool updateLabel)
        {
            InputContainerInfo = inputContainerInfo;
            OutputContainerInfo = outputContainerInfo;
            DispenserName = dispenserName;
            BufferReagents = bufferReagents;
            Mode = mode;
            BufferCurrentVolume = bufferCurrentVolume;
            SelectedPillType = selectedPillType;
            PillProductionLimit = pillProductionLimit;
            BottleProductionLimit = bottleProductionLimit;
            UpdateLabel = updateLabel;
        }
    }

    [Serializable, NetSerializable]
    public enum ChemMasterUiKey
    {
        Key
    }
}
