using Content.Shared.Kitchen.Components;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen.EntitySystems;

public abstract partial class SharedMicrowaveSystem
{
    /// <summary>
    ///     Subscribe to UI-related events for microwaves.
    /// </summary>
    private void InitializeUI()
    {
        SubscribeLocalEvent<MicrowaveComponent, MicrowaveStartCookMessage>((e, ref m) => StartCooking(e, m.Actor));
    }

    /// <summary>
    ///     Ejects all ingredients from the microwave.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnEjectAll(Entity<MicrowaveComponent> ent, ref MicrowaveEjectMessage args)
    {
        if (!HasContents(ent.AsNullable()) || IsActiveMicrowave(ent.AsNullable()))
            return;

        ContainerSys.EmptyContainer(ent.Comp.Storage);
        AudioSys.PlayPredicted(ent.Comp.ClickSound, ent, args.Actor, AudioParams.Default.WithVolume(-2));
    }

    /// <summary>
    ///     Ejects an ingredient entity from the microwave.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnEjectSolidIndexed(Entity<MicrowaveComponent> ent, ref MicrowaveEjectSolidIndexedMessage args)
    {
        if (!HasContents(ent.AsNullable()) || IsActiveMicrowave(ent.AsNullable()))
            return;

        ContainerSys.Remove(GetEntity(args.EntityId), ent.Comp.Storage);
        AudioSys.PlayPredicted(ent.Comp.ClickSound, ent, args.Actor, AudioParams.Default.WithVolume(-2));
    }

    /// <summary>
    ///     Change the cook time of the microwave by selecting a new button index.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnSelectCookTime(Entity<MicrowaveComponent> ent, ref MicrowaveSelectCookTimeMessage args)
    {
        if (!HasContents(ent.AsNullable())
            || IsActiveMicrowave(ent.AsNullable())
            || !_power.IsPowered(ent.Owner))
            return;

        // some validation to prevent trollage
        if (args.NewCookTime % 5 != 0 || args.NewCookTime > ent.Comp.MaxCookTime)
            return;

        ent.Comp.CurrentCookTimeButtonIndex = args.ButtonIndex;
        ent.Comp.CurrentCookTimerTime = args.NewCookTime;
        Dirty(ent);

        AudioSys.PlayPredicted(ent.Comp.ClickSound, ent, args.Actor, AudioParams.Default.WithVolume(-2));
        UpdateUserInterfaceState(ent.AsNullable());
    }

    /// <summary>
    ///     Update the UI state of the microwave, including the microwave's current contents, cook time,
    ///     and whether or not it is actively cooking.
    /// </summary>
    /// <param name="microwave">The microwave to update.</param>
    public virtual void UpdateUserInterfaceState(Entity<MicrowaveComponent?> microwave)
    {

    }
}

/// <summary>
///     Sent from client to server to request the microwave to start cooking.
/// </summary>
[Serializable, NetSerializable]
public sealed class MicrowaveStartCookMessage : BoundUserInterfaceMessage;

/// <summary>
///     Sent from client to server to request ejecting all contents of the microwave.
/// </summary>
[Serializable, NetSerializable]
public sealed class MicrowaveEjectMessage : BoundUserInterfaceMessage;

/// <summary>
///     Sent from client to server to request ejecting an entity from the microwave.
/// </summary>
[Serializable, NetSerializable]
public sealed class MicrowaveEjectSolidIndexedMessage(NetEntity entityId) : BoundUserInterfaceMessage
{
    /// <summary>
    ///     The entity to eject from the microwave.
    /// </summary>
    public NetEntity EntityId = entityId;
}

/// <summary>
///     Sent from client to server to request changing the selected cook time button of the microwave.
/// </summary>
[Serializable, NetSerializable]
public sealed class MicrowaveSelectCookTimeMessage(int buttonIndex, uint inputTime) : BoundUserInterfaceMessage
{
    /// <summary>
    ///     The index of the cook time button to select.
    /// </summary>
    public int ButtonIndex = buttonIndex;

    /// <summary>
    ///     The cooking time associated with the newly-selected button.
    /// </summary>
    public uint NewCookTime = inputTime;
}
