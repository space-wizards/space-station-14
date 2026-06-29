using Content.Shared.Kitchen.Components;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Kitchen.EntitySystems;

/// <summary>
///     A system that handles microwave logic, such as activation, malfunctions, and producing cooked recipes.
///     TODO: Replace with a more sophisticated(?) cooking system.
/// </summary>
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
            var timeElapsed = curTime - active.LastCookUpdated;
            var dirty = false;

            // Roll malfunctions
            if (active.NextMalfunction < curTime)
            {
                RollMalfunction((uid, microwave));
                active.NextMalfunction += microwave.MalfunctionInterval;
                dirty = true;
            }

            // Finish cooking
            if (active.CookTimeEnd < curTime)
            {
                AddTemperature(microwave, (float)timeElapsed.TotalSeconds);
                CompleteCooking((uid, active, microwave));
                Dirty(uid, active);
                continue;
            }

            // Otherwise, process the cooking cycle
            if (active.NextCookUpdate < curTime)
            {
                active.NextCookUpdate += active.CookUpdateInterval;
                active.LastCookUpdated = curTime;
                AddTemperature(microwave, (float)timeElapsed.TotalSeconds);
                dirty = true;
            }

            if (dirty)
                Dirty(uid, active);
        }
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
public sealed class MicrowaveEjectSolidIndexedMessage(NetEntity entityId) : BoundUserInterfaceMessage
{
    /// <summary>
    ///     The entity to eject from the microwave.
    /// </summary>
    public NetEntity EntityID = entityId;
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

/// <summary>
///     Sent from server to client to display a list of items, whether or not the microwave is active, and the current cook time.
/// </summary>
[NetSerializable, Serializable]
public sealed class MicrowaveUpdateUserInterfaceState(
    NetEntity[] containedSolids,
    bool isMicrowaveBusy,
    int activeButtonIndex,
    uint currentCookTime,
    TimeSpan currentCookTimeEnd) : BoundUserInterfaceState
{
    /// <summary>
    ///     A list of microwave entity contents.
    /// </summary>
    public NetEntity[] ContainedSolids = containedSolids;

    /// <summary>
    ///     Whether or not the microwave is currently running.
    /// </summary>
    public bool IsMicrowaveBusy = isMicrowaveBusy;

    /// <summary>
    ///     The currently-selected cook time button.
    /// </summary>
    public int ActiveButtonIndex = activeButtonIndex;

    /// <summary>
    ///     The amount of time remaining on the microwave.
    /// </summary>
    public uint CurrentCookTime = currentCookTime;

    /// <summary>
    ///     The time that this microwave will stop cooking.
    /// </summary>
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
