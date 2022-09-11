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
        public const string ContainerSlotName = "beakerSlot";
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

    [Serializable, NetSerializable]
    public sealed class ChemMasterBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly FixedPoint2? ContainerCurrentVolume;
        public readonly FixedPoint2? ContainerMaxVolume;
        public readonly string? ContainerName;

        /// <summary>
        /// A list of the reagents and their amounts within the beaker/reagent container, if applicable.
        /// </summary>
        public readonly IReadOnlyList<Solution.ReagentQuantity>? ContainerReagents;
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

        public ChemMasterBoundUserInterfaceState(FixedPoint2? containerCurrentVolume, FixedPoint2? containerMaxVolume, string? containerName,
            string dispenserName, IReadOnlyList<Solution.ReagentQuantity>? containerReagents, IReadOnlyList<Solution.ReagentQuantity> bufferReagents, ChemMasterMode mode,
            FixedPoint2 bufferCurrentVolume, uint selectedPillType, uint pillProdictionLimit, uint bottleProdictionLimit, bool updateLabel)
        {
            ContainerCurrentVolume = containerCurrentVolume;
            ContainerMaxVolume = containerMaxVolume;
            ContainerName = containerName;
            DispenserName = dispenserName;
            ContainerReagents = containerReagents;
            BufferReagents = bufferReagents;
            Mode = mode;
            BufferCurrentVolume = bufferCurrentVolume;
            SelectedPillType = selectedPillType;
            PillProductionLimit = pillProdictionLimit;
            BottleProductionLimit = bottleProdictionLimit;
            UpdateLabel = updateLabel;
        }

        public bool HasContainer()
        {
            return ContainerCurrentVolume is not null;
        }
    }

    [Serializable, NetSerializable]
    public enum ChemMasterUiKey
    {
        Key
    }
}
