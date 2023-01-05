using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    /// <summary>
    /// This class holds constants that are shared between client and server.
    /// </summary>
    public sealed class SharedCentrifuge
    {
        public const string BufferSolutionName = "buffer";
        public const string InputSlotName = "Centrifuge-beakerSlot";
        public const string OutputSlotName = "Centrifuge-outputSlot";

        [Serializable, NetSerializable]
        public enum CentrifugeVisualState : byte
        {
            BeakerAttached,
            OutputAttached
        }
    }

    [Serializable, NetSerializable]
    public sealed class CentrifugeSetModeMessage : BoundUserInterfaceMessage
    {
        public readonly CentrifugeMode CentrifugeMode;

        public CentrifugeSetModeMessage(CentrifugeMode mode)
        {
            CentrifugeMode = mode;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CentrifugeReagentAmountButtonMessage : BoundUserInterfaceMessage
    {
        public readonly string ReagentId;
        public readonly CentrifugeReagentAmount Amount;
        public readonly bool FromBuffer;

        public CentrifugeReagentAmountButtonMessage(string reagentId, CentrifugeReagentAmount amount, bool fromBuffer)
        {
            ReagentId = reagentId;
            Amount = amount;
            FromBuffer = fromBuffer;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CentrifugeActivateButtonMessage : BoundUserInterfaceMessage
    {
        public readonly bool Activated;

        public CentrifugeActivateButtonMessage(bool activated)
        {
            Activated = activated;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CentrifugeElectrolysisButtonMessage : BoundUserInterfaceMessage
    {
        public readonly bool Activated;

        public CentrifugeElectrolysisButtonMessage(bool activated)
        {
            Activated = activated;
        }
    }

    public enum CentrifugeMode
    {
        Transfer,
        Discard,
    }

    public enum CentrifugeReagentAmount
    {
        U1 = 1,
        U5 = 5,
        U10 = 10,
        U25 = 25,
        All,
    }

    public static class CentrifugeReagentAmountToFixedPoint
    {
        public static FixedPoint2 GetFixedPoint(this CentrifugeReagentAmount amount)
        {
            if (amount == CentrifugeReagentAmount.All)
                return FixedPoint2.MaxValue;
            else
                return FixedPoint2.New((int)amount);
        }
    }

    /// <summary>
    /// Information about the capacity and contents of a container for display in the UI
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class CentrifugeContainerInfo
    {
        /// <summary>
        /// The container name to show to the player
        /// </summary>
        public readonly string DisplayName;
        /// <summary>
        /// Whether the container holds reagents or entities
        /// </summary>
        public readonly bool HoldsReagents;
        /// <summary>
        /// The currently used volume of the container
        /// </summary>
        public readonly FixedPoint2 CurrentVolume;
        /// <summary>
        /// The maximum volume of the container
        /// </summary>
        public readonly FixedPoint2 MaxVolume;
        /// <summary>
        /// A list of the reagents/entities and their sizes within the container
        /// </summary>
        // todo: this causes NetSerializer exceptions if it's an IReadOnlyList (which would be preferred)
        public readonly List<(string Id, FixedPoint2 Quantity)> Contents;

        public CentrifugeContainerInfo(
            string displayName, bool holdsReagents,
            FixedPoint2 currentVolume, FixedPoint2 maxVolume,
            List<(string, FixedPoint2)> contents)
        {
            DisplayName = displayName;
            HoldsReagents = holdsReagents;
            CurrentVolume = currentVolume;
            MaxVolume = maxVolume;
            Contents = contents;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CentrifugeBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly CentrifugeContainerInfo? InputContainerInfo;
        public readonly CentrifugeContainerInfo? OutputContainerInfo;

        /// <summary>
        /// A list of the reagents and their amounts within the buffer, if applicable.
        /// </summary>
        public readonly IReadOnlyList<Solution.ReagentQuantity> BufferReagents;

        public readonly CentrifugeMode Mode;

        public readonly FixedPoint2? BufferCurrentVolume;

        public CentrifugeBoundUserInterfaceState(
            CentrifugeMode mode, CentrifugeContainerInfo? inputContainerInfo, CentrifugeContainerInfo? outputContainerInfo,
            IReadOnlyList<Solution.ReagentQuantity> bufferReagents, FixedPoint2 bufferCurrentVolume)
        {
            InputContainerInfo = inputContainerInfo;
            OutputContainerInfo = outputContainerInfo;
            BufferReagents = bufferReagents;
            Mode = mode;
            BufferCurrentVolume = bufferCurrentVolume;
        }
    }

    [Serializable, NetSerializable]
    public enum CentrifugeUiKey
    {
        Key
    }
}
