using System;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen.Components
{
    public abstract class SharedReagentGrinderComponent : Component
    {
        [Serializable, NetSerializable]
        public class ReagentGrinderGrindStartMessage : BoundUserInterfaceMessage
        {
            public ReagentGrinderGrindStartMessage()
            {
            }
        }

        [Serializable, NetSerializable]
        public class ReagentGrinderJuiceStartMessage : BoundUserInterfaceMessage
        {
            public ReagentGrinderJuiceStartMessage()
            {
            }
        }

        [Serializable, NetSerializable]
        public class ReagentGrinderEjectChamberAllMessage : BoundUserInterfaceMessage
        {
            public ReagentGrinderEjectChamberAllMessage()
            {
            }
        }

        [Serializable, NetSerializable]
        public class ReagentGrinderEjectBeakerMessage : BoundUserInterfaceMessage
        {
            public ReagentGrinderEjectBeakerMessage()
            {
            }
        }

        [Serializable, NetSerializable]
        public class ReagentGrinderEjectChamberContentMessage : BoundUserInterfaceMessage
        {
            public EntityUid EntityID;
            public ReagentGrinderEjectChamberContentMessage(EntityUid entityID)
            {
                EntityID = entityID;
            }
        }

        [Serializable, NetSerializable]
        public class ReagentGrinderWorkStartedMessage : BoundUserInterfaceMessage
        {
            public GrinderProgram GrinderProgram;
            public ReagentGrinderWorkStartedMessage(GrinderProgram grinderProgram)
            {
                GrinderProgram = grinderProgram;
            }
        }

        [Serializable, NetSerializable]
        public class ReagentGrinderWorkCompleteMessage : BoundUserInterfaceMessage
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

        [NetSerializable, Serializable]
        public enum ReagentGrinderUiKey : byte
        {
            Key
        }

        [Serializable, NetSerializable]
        public enum GrinderProgram : byte
        {
            Grind,
            Juice
        }
    }

    [NetSerializable, Serializable]
    public sealed class ReagentGrinderInterfaceState : BoundUserInterfaceState
    {
        public bool IsBusy;
        public bool HasBeakerIn;
        public bool Powered;
        public bool CanJuice;
        public bool CanGrind;
        public EntityUid[] ChamberContents;
        public Solution.ReagentQuantity[]? ReagentQuantities;
        public ReagentGrinderInterfaceState(bool isBusy, bool hasBeaker, bool powered, bool canJuice, bool canGrind, EntityUid[] chamberContents, Solution.ReagentQuantity[]? heldBeakerContents)
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
