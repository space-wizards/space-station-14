using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Stacks;
using Content.Shared.Temperature.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Kitchen.EntitySystems;

/// <summary>
/// A system that handles microwave logic, such as activation, malfunctions, and producing cooked recipes.
/// TODO: Replace with a more sophisticated(?) cooking system.
/// </summary>
public abstract partial class MicrowaveSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected SharedAppearanceSystem Appearance = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] protected SharedContainerSystem Container = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] protected SharedSolutionContainerSystem Solution = default!;

    [Dependency] private INetManager _net = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private RecipeSystem _recipes = default!;
    [Dependency] private SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedItemSystem _item = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedPowerStateSystem _powerState = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private SharedTemperatureSystem _temperature = default!;

    [Dependency] protected EntityQuery<MicrowaveComponent> MicrowaveQuery;
    [Dependency] protected EntityQuery<ActiveMicrowaveComponent> ActiveMicrowaveQuery;
    [Dependency] protected EntityQuery<ItemComponent> ItemQuery;
    [Dependency] protected EntityQuery<StackComponent> StackQuery;

    public override void Initialize()
    {
        base.Initialize();

        InitializeUI();
    }

    /// <summary>
    /// Processes every active microwave's ongoing cooking operation.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = Timing.CurTime;

        var query = EntityQueryEnumerator<ActiveMicrowaveComponent, MicrowaveComponent>();
        while (query.MoveNext(out var uid, out var active, out var microwave))
        {
            // Roll malfunctions
            if (active.Malfunctioning && active.NextMalfunction < curTime)
            {
                active.NextMalfunction += microwave.MalfunctionInterval;
                DirtyField(uid, active, nameof(active.NextMalfunction));

                RollMalfunction((uid, microwave));
            }

            var timeSinceUpdate = curTime - active.LastCookUpdated;
            var timeElapsed = (float)timeSinceUpdate.TotalSeconds;

            // Finish cooking
            if (active.CookTimeEnd < curTime)
            {
                AddTemperature((uid, microwave), timeElapsed);
                CompleteCooking((uid, active, microwave));
                continue;
            }

            // Otherwise, process the cooking cycle
            if (active.NextCookUpdate > curTime)
                continue;

            active.NextCookUpdate += microwave.UpdateInterval;
            active.LastCookUpdated = curTime;
            DirtyFields(uid, active, null, nameof(active.NextCookUpdate), nameof(active.LastCookUpdated));
            AddTemperature((uid, microwave), timeElapsed);
        }
    }

    /// <summary>
    /// Adds an "on" port to this microwave.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<MicrowaveComponent> ent, ref MapInitEvent args)
    {
        _deviceLink.EnsureSinkPorts(ent, ent.Comp.OnPort);
    }

    /// <summary>
    /// When a microwave is broken, its appearance changes and it stops being usable for cooking.
    /// It will stop any ongoing cooking operations and empty its contents.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBreak(Entity<MicrowaveComponent> ent, ref BreakageEventArgs args)
    {
        ent.Comp.Broken = true;
        DirtyField(ent.AsNullable(), nameof(ent.Comp.Broken));
        SetAppearance(ent, MicrowaveVisualState.Broken);

        StopCooking(ent);
        Container.EmptyContainer(ent.Comp.Storage);
        UpdateUI(ent.AsNullable());
    }

    /// <summary>
    /// Stop cooking if the microwave loses power.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<MicrowaveComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            SetAppearance(ent, MicrowaveVisualState.Idle);
            StopCooking(ent);
        }

        UpdateUI(ent.AsNullable());
    }

    /// <summary>
    /// Empty the microwave if it is unanchored.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnAnchorChanged(Entity<MicrowaveComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            Container.EmptyContainer(ent.Comp.Storage);
    }

    /// <summary>
    /// Turns the microwave on if its "on" port is activated.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<MicrowaveComponent> ent, ref SignalReceivedEvent args)
    {
        if (ent.Comp.Broken || !_power.IsPowered(ent.Owner))
            return;

        if (args.Port == ent.Comp.OnPort)
            StartCooking(ent, null);
    }

    /// <summary>
    /// Updates the microwave's appearance state.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    /// <param name="state">The visual state of the microwave.</param>
    private void SetAppearance(Entity<MicrowaveComponent> ent,
        MicrowaveVisualState state)
    {
        var display = ent.Comp.Broken ? MicrowaveVisualState.Broken : state;
        Appearance.SetData(ent.Owner,
            PowerDeviceVisuals.VisualState,
            display);
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
