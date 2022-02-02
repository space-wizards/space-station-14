using System;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen.Components
{

    [NetworkedComponent()]
    public class SharedMicrowaveComponent : Component
    {
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
        public class MicrowaveVaporizeReagentIndexedMessage : BoundUserInterfaceMessage
        {

            public Solution.ReagentQuantity ReagentQuantity;
            public MicrowaveVaporizeReagentIndexedMessage(Solution.ReagentQuantity reagentQuantity)
            {
                ReagentQuantity = reagentQuantity;
            }
        }
        [Serializable, NetSerializable]
        public class MicrowaveSelectCookTimeMessage : BoundUserInterfaceMessage
        {
            public int ButtonIndex;
            public uint NewCookTime;
            public MicrowaveSelectCookTimeMessage(int buttonIndex, uint inputTime)
            {
                ButtonIndex = buttonIndex;
                NewCookTime = inputTime;
            }
        }
    }

    [NetSerializable, Serializable]
    public class MicrowaveUpdateUserInterfaceState : BoundUserInterfaceState
    {
        public Solution.ReagentQuantity[] ReagentQuantities;
        public EntityUid[] ContainedSolids;
        public bool IsMicrowaveBusy;
        public int ActiveButtonIndex;
        public uint CurrentCookTime;

        public MicrowaveUpdateUserInterfaceState(Solution.ReagentQuantity[] reagents, EntityUid[] containedSolids,
            bool isMicrowaveBusy, int activeButtonIndex, uint currentCookTime)
        {
            ReagentQuantities = reagents;
            ContainedSolids = containedSolids;
            IsMicrowaveBusy = isMicrowaveBusy;
            ActiveButtonIndex = activeButtonIndex;
            CurrentCookTime = currentCookTime;
        }

    }

    [Serializable, NetSerializable]
    public enum MicrowaveVisualState
    {
        Idle,
        Cooking,
        Broken
    }

    [NetSerializable, Serializable]
    public enum MicrowaveUiKey
    {
        Key
    }

}
