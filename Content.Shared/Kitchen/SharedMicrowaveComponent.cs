using System;
using System.Collections.Generic;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.GameObjects.Components;
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
    }

    [NetSerializable, Serializable]
    public class MicrowaveUserInterfaceState : BoundUserInterfaceState
    {
        public readonly List<Solution.ReagentQuantity> ReagentsReagents;
        public readonly Dictionary<string, int> ContainedSolids;
        public MicrowaveUserInterfaceState(List<Solution.ReagentQuantity> reagents, Dictionary<string,int> solids)
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
