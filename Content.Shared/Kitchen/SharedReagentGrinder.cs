using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen
{
    public sealed class SharedReagentGrinder
    {
        public static string BeakerSlotId = "beakerSlot";

        public static string InputContainerId = "inputContainer";
    }

    [Serializable, NetSerializable]
    public sealed class ReagentGrinderStartMessage : BoundUserInterfaceMessage
    {
        public readonly GrinderProgram Program;
        public ReagentGrinderStartMessage(GrinderProgram program)
        {
            Program = program;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ReagentGrinderEjectChamberAllMessage : BoundUserInterfaceMessage
    {
        public ReagentGrinderEjectChamberAllMessage()
        {
        }
    }

    [Serializable, NetSerializable]
    public sealed class ReagentGrinderEjectChamberContentMessage : BoundUserInterfaceMessage
    {
        public NetEntity EntityId;
        public ReagentGrinderEjectChamberContentMessage(NetEntity entityId)
        {
            EntityId = entityId;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ReagentGrinderWorkStartedMessage : BoundUserInterfaceMessage
    {
        public GrinderProgram GrinderProgram;
        public ReagentGrinderWorkStartedMessage(GrinderProgram grinderProgram)
        {
            GrinderProgram = grinderProgram;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ReagentGrinderWorkCompleteMessage : BoundUserInterfaceMessage
    {
        public ReagentGrinderWorkCompleteMessage()
        {
        }
    }

    [Serializable, NetSerializable]
    public enum ReagentGrinderVisualState : byte
    {
        BeakerAttached
    }

    [Serializable, NetSerializable]
    public enum GrinderProgram : byte
    {
        Grind,
        Juice
    }

    [NetSerializable, Serializable]
    public enum ReagentGrinderUiKey : byte
    {
        Key
    }

    [NetSerializable, Serializable]
    public sealed class ReagentGrinderInterfaceState : BoundUserInterfaceState
    {
        public bool IsBusy;
        public bool HasBeakerIn;
        public bool Powered;
        public bool CanJuice;
        public bool CanGrind;
        public NetEntity[] ChamberContents;
        public ReagentQuantity[]? ReagentQuantities;
        public ReagentGrinderInterfaceState(bool isBusy, bool hasBeaker, bool powered, bool canJuice, bool canGrind, NetEntity[] chamberContents, ReagentQuantity[]? heldBeakerContents)
        {
            IsBusy = isBusy;
            HasBeakerIn = hasBeaker;
            Powered = powered;
            CanJuice = canJuice;
            CanGrind = canGrind;
            ChamberContents = chamberContents;
            ReagentQuantities = heldBeakerContents;
        }
    }
}
