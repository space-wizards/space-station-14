using System;
using System.Collections.Generic;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.GameObjects.Components.UserInterface;

namespace Content.Shared.Kitchen
{

    public class SharedMicrowaveComponent : Component
    {

        public override string Name => "Microwave";
        public override uint? NetID => ContentNetIDs.MICROWAVE;

        [Serializable, NetSerializable]
        public class MicrowaveStartCookMessage : BoundUserInterfaceMessage
        {
            public MicrowaveStartCookMessage()
            {
            }
        }

        [Serializable, NetSerializable]
        public class MicrowaveEjectMessage : BoundUserInterfaceMessage
        {
            public MicrowaveEjectMessage()
            {
            }
        }

        [Serializable, NetSerializable]
        public class MicrowaveEjectSolidIndexedMessage : BoundUserInterfaceMessage
        {

            public EntityUid EntityID;
            public MicrowaveEjectSolidIndexedMessage(EntityUid entityID)
            {
                EntityID = entityID;
            }
        }
        [Serializable, NetSerializable]
        public class MicrowaveSelectCookTimeMessage : BoundUserInterfaceMessage
        {
            public uint newCookTime;
            public MicrowaveSelectCookTimeMessage(uint inputTime)
            {
                newCookTime = inputTime;
            }
        }
    }



    [NetSerializable, Serializable]
    public class MicrowaveUpdateUserInterfaceState : BoundUserInterfaceState
    {
        public readonly IReadOnlyList<Solution.ReagentQuantity> ReagentsReagents;
        public readonly List<EntityUid> ContainedSolids;
        public MicrowaveUpdateUserInterfaceState(IReadOnlyList<Solution.ReagentQuantity> reagents, List<EntityUid> solids)
        {
            ReagentsReagents = reagents;
            ContainedSolids = solids;
        }
    }

    [Serializable, NetSerializable]
    public enum MicrowaveVisualState
    {
        Idle,
        Cooking
    }

    [NetSerializable, Serializable]
    public enum MicrowaveUiKey
    {
        Key
    }

}
