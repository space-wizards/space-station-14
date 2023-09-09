using Content.Shared.Chemistry.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen.Components
{
    [Serializable, NetSerializable]
    public sealed class MicrowaveStartCookMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class MicrowaveEjectMessage : BoundUserInterfaceMessage
    {

    }

    [Serializable, NetSerializable]
    public sealed class MicrowaveEjectSolidIndexedMessage : BoundUserInterfaceMessage
    {
        public EntityUid EntityID;
        public MicrowaveEjectSolidIndexedMessage(EntityUid entityId)
        {
            EntityID = entityId;
        }
    }

    [Serializable, NetSerializable]
    public sealed class MicrowaveVaporizeReagentIndexedMessage : BoundUserInterfaceMessage
    {
        public Solution.ReagentQuantity ReagentQuantity;
        public MicrowaveVaporizeReagentIndexedMessage(Solution.ReagentQuantity reagentQuantity)
        {
            ReagentQuantity = reagentQuantity;
        }
    }

    [Serializable, NetSerializable]
    public sealed class MicrowaveSelectCookTimeMessage : BoundUserInterfaceMessage
    {
        public int ButtonIndex;
        public uint NewCookTime;
        public MicrowaveSelectCookTimeMessage(int buttonIndex, uint inputTime)
        {
            ButtonIndex = buttonIndex;
            NewCookTime = inputTime;
        }
    }

    [NetSerializable, Serializable]
    public sealed class MicrowaveUpdateUserInterfaceState : BoundUserInterfaceState
    {
        public EntityUid[] ContainedSolids;
        public bool IsMicrowaveBusy;
        public int ActiveButtonIndex;
        public uint CurrentCookTime;

        public MicrowaveUpdateUserInterfaceState(EntityUid[] containedSolids,
            bool isMicrowaveBusy, int activeButtonIndex, uint currentCookTime)
        {
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
