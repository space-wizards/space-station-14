using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen.Components;

/// <summary>
///     Sent from client to server to request the microwave to start cooking.
/// </summary>
[Serializable, NetSerializable]
public sealed class MicrowaveStartCookMessage : BoundUserInterfaceMessage
{ }

/// <summary>
///     Sent from client to server to request ejecting all contents of the microwave.
/// </summary>
[Serializable, NetSerializable]
public sealed class MicrowaveEjectMessage : BoundUserInterfaceMessage
{ }

/// <summary>
///     Sent from client to server to request ejecting an entity from the microwave.
/// </summary>
[Serializable, NetSerializable]
public sealed class MicrowaveEjectSolidIndexedMessage : BoundUserInterfaceMessage
{
    /// <summary>
    ///     The entity to eject from the microwave.
    /// </summary>
    public NetEntity EntityID;
    public MicrowaveEjectSolidIndexedMessage(NetEntity entityId)
    {
        EntityID = entityId;
    }
}

/// <summary>
///     Unused.
/// </summary>
[Serializable, NetSerializable]
public sealed class MicrowaveVaporizeReagentIndexedMessage : BoundUserInterfaceMessage
{
    public ReagentQuantity ReagentQuantity;
    public MicrowaveVaporizeReagentIndexedMessage(ReagentQuantity reagentQuantity)
    {
        ReagentQuantity = reagentQuantity;
    }
}

/// <summary>
///     Sent from client to server to request changing the selected cook time button of the microwave.
/// </summary>
[Serializable, NetSerializable]
public sealed class MicrowaveSelectCookTimeMessage : BoundUserInterfaceMessage
{
    /// <summary>
    ///     The index of the cook time button to select.
    /// </summary>
    public int ButtonIndex;

    /// <summary>
    ///     The cooking time associated with the newly-selected button.
    /// </summary>
    public uint NewCookTime;

    public MicrowaveSelectCookTimeMessage(int buttonIndex, uint inputTime)
    {
        ButtonIndex = buttonIndex;
        NewCookTime = inputTime;
    }
}

/// <summary>
///     Sent from server to client to display a list of items, whether or not the microwave is active, and the current cook time.
/// </summary>
[NetSerializable, Serializable]
public sealed class MicrowaveUpdateUserInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    ///     A list of microwave entity contents.
    /// </summary>
    public NetEntity[] ContainedSolids;

    /// <summary>
    ///     Whether or not the microwave is currently running.
    /// </summary>
    public bool IsMicrowaveBusy;

    /// <summary>
    ///     The currently-selected cook time button.
    /// </summary>
    public int ActiveButtonIndex;

    /// <summary>
    ///     The amount of time remaining on the microwave.
    /// </summary>
    public uint CurrentCookTime;

    /// <summary>
    ///     The time that this microwave will stop cooking.
    /// </summary>
    public TimeSpan CurrentCookTimeEnd;

    public MicrowaveUpdateUserInterfaceState(NetEntity[] containedSolids,
        bool isMicrowaveBusy, int activeButtonIndex, uint currentCookTime, TimeSpan currentCookTimeEnd)
    {
        ContainedSolids = containedSolids;
        IsMicrowaveBusy = isMicrowaveBusy;
        ActiveButtonIndex = activeButtonIndex;
        CurrentCookTime = currentCookTime;
        CurrentCookTimeEnd = currentCookTimeEnd;
    }

}

/// <summary>
///     Appearance values for a microwave's visual state.
/// </summary>
[Serializable, NetSerializable]
public enum MicrowaveVisualState
{
    Idle,
    Cooking,
    Broken,
    Bloody
}

/// <summary>
///     UI key for the microwave UI.
/// </summary>
[NetSerializable, Serializable]
public enum MicrowaveUiKey
{
    Key
}
