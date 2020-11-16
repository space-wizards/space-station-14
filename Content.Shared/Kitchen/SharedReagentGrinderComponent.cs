using System;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen
{
    public abstract class SharedReagentGrinderComponent : Component
    {
        public override string Name => "ReagentGrinder";
        public override uint? NetID => ContentNetIDs.REAGENT_GRINDER;


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
        public class ReagentGrinderVaporizeReagentIndexedMessage : BoundUserInterfaceMessage
        {

            public Solution.ReagentQuantity ReagentQuantity;
            public ReagentGrinderVaporizeReagentIndexedMessage(Solution.ReagentQuantity reagentQuantity)
            {
                ReagentQuantity = reagentQuantity;
            }
        }

        [Serializable, NetSerializable]
        public class ReagentGrinderWorkStartedMessage : BoundUserInterfaceMessage
        {
            public bool IsJuiceIntent;

            public ReagentGrinderWorkStartedMessage(bool wasJuiceIntent)
            {
                IsJuiceIntent = wasJuiceIntent;
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
        public enum ReagentGrinderVisualState
        {
            NoBeaker,
            BeakerAttached
        }

        [NetSerializable, Serializable]
        public enum ReagentGrinderUiKey
        {
            Key
        }
    }

    [NetSerializable, Serializable]
    public sealed class ReagentGrinderInterfaceState : BoundUserInterfaceState
    {
        public bool IsBusy;
        public bool HasBeakerIn;
        public bool CanJuice;
        public bool CanGrind;
        public EntityUid[] ChamberContents;
        public Solution.ReagentQuantity[] ReagentQuantities;
        public ReagentGrinderInterfaceState(bool isBusy, bool hasBeaker, bool canJuice, bool canGrind, EntityUid[] chamberContents, Solution.ReagentQuantity[] heldBeakerContents)
        {
            IsBusy = isBusy;
            HasBeakerIn = hasBeaker;
            CanJuice = canJuice;
            CanGrind = canGrind;
            ChamberContents = chamberContents;
            ReagentQuantities = heldBeakerContents;
        }
    }
}
