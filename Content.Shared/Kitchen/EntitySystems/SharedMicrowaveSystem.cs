using Content.Shared.Kitchen.Components;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen.EntitySystems;

public abstract partial class SharedMicrowaveSystem : EntitySystem
{
    [Dependency] protected SharedAppearanceSystem Appearance = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedPowerStateSystem _powerState = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveMicrowaveComponent, ComponentStartup>(OnCookStart);
        SubscribeLocalEvent<ActiveMicrowaveComponent, ComponentShutdown>(OnCookStop);
        SubscribeLocalEvent<ActiveMicrowaveComponent, EntInsertedIntoContainerMessage>(OnActiveMicrowaveInsert);
        SubscribeLocalEvent<ActiveMicrowaveComponent, EntRemovedFromContainerMessage>(OnActiveMicrowaveRemove);
    }

    /// <summary>
    ///     Updates the microwave's appearance state.
    /// </summary>
    /// <param name="uid">The microwave entity.</param>
    /// <param name="state">The visual state of the microwave.</param>
    /// <param name="component">The entity's microwave component.</param>
    /// <param name="appearanceComponent">The microwave's appearance component.</param>
    private void SetAppearance(Entity<MicrowaveComponent?> ent,
        MicrowaveVisualState state,
        AppearanceComponent? appearanceComponent = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, ref appearanceComponent, logMissing: false))
            return;

        var display = ent.Comp.Broken ? MicrowaveVisualState.Broken : state;
        Appearance.SetData(ent.Owner,
            PowerDeviceVisuals.VisualState,
            display,
            appearanceComponent);
    }
}

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
