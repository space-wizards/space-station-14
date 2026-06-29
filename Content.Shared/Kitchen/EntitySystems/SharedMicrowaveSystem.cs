using Content.Shared.Kitchen.Components;
using Content.Shared.Mobs;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Kitchen.EntitySystems;

public abstract partial class SharedMicrowaveSystem : EntitySystem
{
    [Dependency] protected SharedAppearanceSystem Appearance = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedPowerStateSystem _powerState = default!;
    [Dependency] private IGameTiming _timing = default!;
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
    ///     Processes every active microwave's ongoing cooking operation.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<ActiveMicrowaveComponent, MicrowaveComponent>();
        while (query.MoveNext(out var uid, out var active, out var microwave))
        {
            var dirty = false;

            // Roll malfunctions
            if (active.NextMalfunction > curTime)
            {
                RollMalfunction((uid, microwave));
                active.NextMalfunction += microwave.MalfunctionInterval;
                dirty = true;
            }

            // Process cooking
            if (active.NextUpdate > curTime)
            {
                var timeElapsed = curTime - active.LastUpdated;
                active.NextUpdate += active.UpdateInterval;
                active.LastUpdated = curTime;
                dirty = true;

                UpdateMicrowave((uid, active, microwave), (float)timeElapsed.TotalSeconds);
            }

            if (dirty)
                Dirty(uid, active);
        }
    }

    /// <summary>
    ///     Heats up the contents of an active microwave. Has a random chance of exploding if the microwave
    ///     is currently malfunctioning. Also finishes cooking, if the microwave timer expires.
    /// </summary>
    /// <remarks>
    ///     This is called once per frame by <see cref="Update"/>.
    /// </remarks>
    /// <param name="ent">The microwave entity.</param>
    /// <param name="time">The amount of time elapsed.</param>
    private void UpdateMicrowave(Entity<ActiveMicrowaveComponent, MicrowaveComponent> ent, float time)
    {
        var active = ent.Comp1;
        var microwave = ent.Comp2;
        var curTime = _timing.CurTime;

        AddTemperature(microwave, time);

        if (active.CookTimeEnd < curTime)
            CompleteCooking(ent);
    }

    /// <summary>
    ///     Adds temperature to every item in the microwave based on the time it took to microwave.
    /// </summary>
    /// <param name="component">The microwave that is heating up.</param>
    /// <param name="time">The heating time that has elapsed, in seconds.</param>
    protected virtual void AddTemperature(MicrowaveComponent component, float time)
    { }

    /// <summary>
    ///     Attempts to roll random "malfunction" events on a malfunctioning microwave.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    protected virtual void RollMalfunction(Entity<MicrowaveComponent> ent)
    { }

    /// <summary>
    ///     Finishes a cooking operation in the microwave, resulting in a finished food recipe,
    ///     the ejection of all remaining ingredients, and a sound cue.
    /// </summary>
    /// <param name="ent">The micorawve entity.</param>
    private void CompleteCooking(Entity<ActiveMicrowaveComponent, MicrowaveComponent> ent)
    {
        var active = ent.Comp1;
        var microwave = ent.Comp2;
        var microwaveEnt = (ent.Owner, microwave);

        if (active.PortionedRecipe.Recipe != null)
            ProduceFinishedRecipe(microwaveEnt, active.PortionedRecipe.Recipe, active.PortionedRecipe.Count);

        microwave.CurrentCookTimeEnd = TimeSpan.Zero;
        _container.EmptyContainer(microwave.Storage);

        Audio.PlayPredicted(microwave.FoodDoneSound, ent, null); // beep... beep... beep
        UpdateUserInterfaceState(microwaveEnt);
        StopCooking(microwaveEnt);
    }

    /// <summary>
    ///     Removes components from a microwave and its contents related to active microwave use.
    /// </summary>
    /// <remarks>
    ///     When the ActiveMicrowaveComponent is removed, it will trigger <see cref="OnCookStop"/> on shutdown.
    /// </remarks>
    /// <param name="ent">The microwave entity.</param>
    private void StopCooking(Entity<MicrowaveComponent> ent)
    {
        RemCompDeferred<ActiveMicrowaveComponent>(ent);

        foreach (var solid in ent.Comp.Storage.ContainedEntities)
            RemCompDeferred<ActivelyMicrowavedComponent>(solid);
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
