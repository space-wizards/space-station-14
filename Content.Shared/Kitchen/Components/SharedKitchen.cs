using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen.Components;

// Starlight-start

[Serializable, NetSerializable]
public sealed class MicrowaveStartCookMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class MicrowaveStopCookMessage : BoundUserInterfaceMessage
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
public sealed class MicrowaveUpdateUserInterfaceState : BoundUserInterfaceState
{
    public NetEntity[] ContainedSolids;
    public bool IsMicrowaveBusy;
    public bool IsMicrowaveSafe;
    public int ActiveButtonIndex;
    public uint CurrentCookTime;

    public TimeSpan CurrentCookTimeEnd;
    public TimeSpan StartedCookTime;

    public MicrowaveUpdateUserInterfaceState(NetEntity[] containedSolids,
        bool isMicrowaveBusy, bool isMicrowaveSafe, int activeButtonIndex, uint currentCookTime, TimeSpan currentCookTimeEnd, TimeSpan startedCookTime)
    {
        ContainedSolids = containedSolids;
        IsMicrowaveBusy = isMicrowaveBusy;
        IsMicrowaveSafe = isMicrowaveSafe;
        ActiveButtonIndex = activeButtonIndex;
        CurrentCookTime = currentCookTime;
        CurrentCookTimeEnd = currentCookTimeEnd;
        StartedCookTime = startedCookTime;
    }

}

[Serializable, NetSerializable]
public enum MicrowaveVisualState
{
    Idle,
    Cooking,
    Broken,
    Bloody
}

[Serializable, NetSerializable]
public enum OpenableKitchenDevice
{
    Opened,
    Closed
}

[NetSerializable, Serializable]
public enum MicrowaveUiKey
{
    Key
}

[Serializable, NetSerializable]
public enum CookingDeviceType
{
    Microwave,
    Oven,
    Stove
}

// Starlight-end: Moved from SharedMicrowave