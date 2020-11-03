using System;
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
        public class ReagentGrinderEjectChamberMessage : BoundUserInterfaceMessage
        {
            public ReagentGrinderEjectChamberMessage()
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

        public ReagentGrinderInterfaceState(bool hasBeaker)
        {
            HasBeakerIn = hasBeaker;
        }
    }
}
