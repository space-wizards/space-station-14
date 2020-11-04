using System;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen
{
    public class SharedReagentGrinderComponent : Component
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
        [NetSerializable, Serializable]
        public enum ReagentGrinderUiKey
        {
            Key
        }
    }

    [NetSerializable, Serializable]
    public sealed class ReagentGrinderInterfaceState : BoundUserInterfaceState
    {
        public bool HasBeakerIn;
        public EntityUid[] ChamberContents;
        public Solution.ReagentQuantity[] ReagentQuantities;


        public ReagentGrinderInterfaceState(bool hasBeaker, EntityUid[] chamberContents, Solution.ReagentQuantity[] heldBeakerContents)
        {
            HasBeakerIn = hasBeaker;
            ChamberContents = chamberContents;
            ReagentQuantities = heldBeakerContents;
        }
    }
}
