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
using Content.Shared.Whitelist;
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
    [Dependency] private SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedItemSystem _item = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedPowerStateSystem _powerState = default!;
    [Dependency] private RecipeManager _recipeManager = default!;
    [Dependency] protected SharedSolutionContainerSystem Solution = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MicrowaveComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MicrowaveComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MicrowaveComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<MicrowaveComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<MicrowaveComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<MicrowaveComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<FoodRecipeProviderComponent, GetSecretRecipesEvent>(OnGetSecretRecipes);

        InitializeActive();
        InitializeContainer();
        InitializeUI();
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

            // Roll malfunctions
            if (active.Malfunctioning && active.NextMalfunction < curTime)
            {
                active.NextMalfunction += microwave.MalfunctionInterval;
                DirtyField(uid, active, nameof(ActiveMicrowaveComponent.NextMalfunction));

                RollMalfunction((uid, microwave));
            }

            // Finish cooking
            if (active.CookTimeEnd < curTime)
            {
                AddTemperature((uid, microwave), (float)timeElapsed.TotalSeconds);
                CompleteCooking((uid, active, microwave));
                continue;
            }

            // Otherwise, process the cooking cycle
            if (active.NextCookUpdate < curTime)
            {
                active.NextCookUpdate += active.CookUpdateInterval;
                active.LastCookUpdated = curTime;
                DirtyField(uid, active, nameof(ActiveMicrowaveComponent.NextCookUpdate));
                DirtyField(uid, active, nameof(ActiveMicrowaveComponent.LastCookUpdated));

                AddTemperature((uid, microwave), (float)timeElapsed.TotalSeconds);
            }
        }
    }

    /// <summary>
    ///     Initializes the microwave's storage container.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnComponentInit(Entity<MicrowaveComponent> ent, ref ComponentInit args)
    {
        // this really does have to be in ComponentInit
        ent.Comp.Storage = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
    }

    /// <summary>
    ///     Adds an "on" port to this microwave.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnMapInit(Entity<MicrowaveComponent> ent, ref MapInitEvent args)
    {
        _deviceLink.EnsureSinkPorts(ent, ent.Comp.OnPort);
    }

    /// <summary>
    ///     When a microwave is broken, its appearance changes and it stops being usable for cooking.
    ///     It will stop any ongoing cooking operations and empty its contents.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnBreak(Entity<MicrowaveComponent> ent, ref BreakageEventArgs args)
    {
        ent.Comp.Broken = true;
        DirtyField(ent.Owner, ent.Comp, nameof(MicrowaveComponent.Broken));
        SetAppearance(ent.AsNullable(), MicrowaveVisualState.Broken);

        StopCooking(ent);
        _container.EmptyContainer(ent.Comp.Storage);
        UpdateUserInterfaceState(ent.AsNullable());
    }

    /// <summary>
    ///     Stop cooking if the microwave loses power.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnPowerChanged(Entity<MicrowaveComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            SetAppearance(ent.AsNullable(), MicrowaveVisualState.Idle);
            StopCooking(ent);
        }

        UpdateUserInterfaceState(ent.AsNullable());
    }

    /// <summary>
    ///     Empty the microwave if it is unanchored.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnAnchorChanged(Entity<MicrowaveComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            _container.EmptyContainer(ent.Comp.Storage);
    }

    /// <summary>
    ///     Turns the microwave on if its "on" port is activated.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnSignalReceived(Entity<MicrowaveComponent> ent, ref SignalReceivedEvent args)
    {
        if (ent.Comp.Broken || !_power.IsPowered(ent.Owner))
            return;

        if (args.Port == ent.Comp.OnPort)
            StartCooking(ent, null);
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
