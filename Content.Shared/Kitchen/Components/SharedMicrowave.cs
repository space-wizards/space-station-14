using Content.Shared.Chemistry.Reagent;
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
        public NetEntity EntityID;
        public MicrowaveEjectSolidIndexedMessage(NetEntity entityId)
        {
            EntityID = entityId;
        }
    }

    [Serializable, NetSerializable]
    public sealed class MicrowaveVaporizeReagentIndexedMessage : BoundUserInterfaceMessage
    {
        public ReagentQuantity ReagentQuantity;
        public MicrowaveVaporizeReagentIndexedMessage(ReagentQuantity reagentQuantity)
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
    public sealed class MicrowaveUpdateUserInterfaceState(
        NetEntity[] containedSolids,
        bool isMicrowaveBusy,
        int activeButtonIndex,
        uint currentCookTime,
        TimeSpan currentCookTimeEnd)
        : BoundUserInterfaceState
    {
        public NetEntity[] ContainedSolids = containedSolids;
        public bool IsMicrowaveBusy = isMicrowaveBusy;
        public int ActiveButtonIndex = activeButtonIndex;
        public uint CurrentCookTime = currentCookTime;
        public TimeSpan CurrentCookTimeEnd = currentCookTimeEnd;
    }

    [Serializable, NetSerializable]
    public enum MicrowaveVisualState
    {
        Idle,
        Cooking,
        Broken,
        Bloody
    }

    [NetSerializable, Serializable]
    public enum MicrowaveUiKey
    {
        Key
    }

}
