using Content.Server.Administration.Logs;
using Content.Server.Construction;
using Content.Server.Explosion.EntitySystems;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Kitchen.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Temperature.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Database;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Destructible;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Robust.Shared.Random;
using Robust.Shared.Audio;
using Content.Server.Lightning;
using Content.Shared.Item;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Stacks;
using Content.Server.Construction.Components;
using Content.Shared.Chat;
using Content.Shared.Damage.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Kitchen.EntitySystems;
using Content.Shared.Whitelist;
using JetBrains.Annotations;

namespace Content.Server.Kitchen.EntitySystems;

/// <summary>
///     A system that handles microwave logic, such as activation, malfunctions, and producing cooked recipes.
///     TODO: Replace with a more sophisticated(?) cooking system.
/// </summary>
public sealed partial class MicrowaveSystem : SharedMicrowaveSystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private DeviceLinkSystem _deviceLink = default!;
    [Dependency] private ExplosionSystem _explosion = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private HandsSystem _handsSystem = default!;
    [Dependency] private SharedItemSystem _item = default!;
    [Dependency] private LightningSystem _lightning = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedPowerStateSystem _powerState = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private RecipeManager _recipeManager = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private SharedSuicideSystem _suicide = default!;
    [Dependency] private TemperatureSystem _temperature = default!;
    [Dependency] private UserInterfaceSystem _userInterface = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MicrowaveComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MicrowaveComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MicrowaveComponent, SolutionChangedEvent>(OnSolutionChange);
        SubscribeLocalEvent<MicrowaveComponent, EntInsertedIntoContainerMessage>(OnContentUpdate);
        SubscribeLocalEvent<MicrowaveComponent, EntRemovedFromContainerMessage>(OnContentUpdate);
        SubscribeLocalEvent<MicrowaveComponent, InteractUsingEvent>(OnInteractUsing, after: new[] { typeof(AnchorableSystem) });
        SubscribeLocalEvent<MicrowaveComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<MicrowaveComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<MicrowaveComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<MicrowaveComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<MicrowaveComponent, SuicideByEnvironmentEvent>(OnSuicideByEnvironment);

        SubscribeLocalEvent<MicrowaveComponent, SignalReceivedEvent>(OnSignalReceived);

        SubscribeLocalEvent<MicrowaveComponent, MicrowaveStartCookMessage>((e, ref m) => Wzhzhzh(e, m.Actor));
        SubscribeLocalEvent<MicrowaveComponent, MicrowaveEjectMessage>(OnEjectMessage);
        SubscribeLocalEvent<MicrowaveComponent, MicrowaveEjectSolidIndexedMessage>(OnEjectIndex);
        SubscribeLocalEvent<MicrowaveComponent, MicrowaveSelectCookTimeMessage>(OnSelectTime);

        SubscribeLocalEvent<ActiveMicrowaveComponent, ComponentStartup>(OnCookStart);
        SubscribeLocalEvent<ActiveMicrowaveComponent, ComponentShutdown>(OnCookStop);
        SubscribeLocalEvent<ActiveMicrowaveComponent, EntInsertedIntoContainerMessage>(OnActiveMicrowaveInsert);
        SubscribeLocalEvent<ActiveMicrowaveComponent, EntRemovedFromContainerMessage>(OnActiveMicrowaveRemove);

        SubscribeLocalEvent<ActivelyMicrowavedComponent, OnConstructionTemperatureEvent>(OnConstructionTemp);
        SubscribeLocalEvent<ActivelyMicrowavedComponent, SolutionRelayEvent<ReactionAttemptEvent>>(OnReactionAttempt);

        SubscribeLocalEvent<FoodRecipeProviderComponent, GetSecretRecipesEvent>(OnGetSecretRecipes);
    }

    /// <summary>
    ///     Processes every active microwave's ongoing cooking operation.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveMicrowaveComponent, MicrowaveComponent>();
        while (query.MoveNext(out var uid, out var active, out var microwave))
            UpdateMicrowave((uid, active, microwave), frameTime);
    }

    /// <summary>
    ///     Helper function to check if a microwave has ingredient contents.
    /// </summary>
    /// <param name="microwave">The microwave entity.</param>
    /// <returns>Whether or not this microwave contains anything.</returns>
    [PublicAPI]
    public static bool HasContents(Entity<MicrowaveComponent> microwave)
    {
        return microwave.Comp.Storage.ContainedEntities.Any();
    }

    /// <summary>
    ///     Initializes the microwave's storage container.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnInit(Entity<MicrowaveComponent> ent, ref ComponentInit args)
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
    ///     Kills the user by microwaving their head.
    /// </summary>
    /// <remarks>
    ///     TODO: Make this not awful, it keeps any items attached to your head still on and you can
    ///     revive someone and cogni them so you have some dumb headless fuck running around. I've seen it happen.
    /// </remarks>
    /// <param name="ent">The microwave entity.</param>
    private void OnSuicideByEnvironment(Entity<MicrowaveComponent> ent, ref SuicideByEnvironmentEvent args)
    {
        if (args.Handled)
            return;

        // The act of getting your head microwaved doesn't actually kill you
        if (!TryComp<DamageableComponent>(args.Victim, out var damageableComponent))
            return;

        // The application of lethal damage is what kills you...
        _suicide.ApplyLethalDamage((args.Victim, damageableComponent), "Heat");

        var victim = args.Victim;
        var othersMessage = Loc.GetString("microwave-component-suicide-others-message", ("victim", victim));
        var selfMessage = Loc.GetString("microwave-component-suicide-message");

        _popupSystem.PopupEntity(othersMessage, victim, Filter.PvsExcept(victim), true);
        _popupSystem.PopupEntity(selfMessage, victim, victim);

        _audio.PlayPvs(ent.Comp.ClickSound, ent.Owner, AudioParams.Default.WithVolume(-2));
        ent.Comp.CurrentCookTimerTime = 10;
        Wzhzhzh(ent, args.Victim);
        UpdateUserInterfaceState(ent);
        args.Handled = true;
    }

    /// <summary>
    ///     When a microwave is broken, its appearance changes and it stops being usable for cooking.
    ///     It will stop any ongoing cooking operations and empty its contents.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void OnBreak(Entity<MicrowaveComponent> ent, ref BreakageEventArgs args)
    {
        ent.Comp.Broken = true;
        SetAppearance(ent.AsNullable(), MicrowaveVisualState.Broken);
        StopCooking(ent);
        _container.EmptyContainer(ent.Comp.Storage);
        UpdateUserInterfaceState(ent);
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

        UpdateUserInterfaceState(ent);
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
        if (ent.Comp.Broken || !_power.IsPowered(ent))
            return;

        if (args.Port == ent.Comp.OnPort)
            Wzhzhzh(ent, null);
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

        active.CookTimeRemaining -= time;
        RollMalfunction(ent);
        AddTemperature(microwave, time);

        if (active.CookTimeRemaining <= 0)
            CompleteCooking(ent);
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
        _appearance.SetData(ent.Owner,
            PowerDeviceVisuals.VisualState,
            display,
            appearanceComponent);
    }

    /// <summary>
    /// Explodes the microwave internally, turning it into a broken state, destroying its board, and spitting out its machine parts
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    public void Explode(Entity<MicrowaveComponent> ent)
    {
        ent.Comp.Broken = true; // Make broken so we stop processing stuff
        _explosion.TriggerExplosive(ent);
        if (TryComp<MachineComponent>(ent, out var machine))
        {
            _container.CleanContainer(machine.BoardContainer);
            _container.EmptyContainer(machine.PartContainer);
        }

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(ent)} exploded from unsafe cooking!");
    }

    /// <summary>
    ///     Attempts to roll random "malfunction" events on a malfunctioning microwave on a given interval
    ///     - usually 1 second. The microwave may shoot sparks or explode.
    /// </summary>
    /// <param name="ent">The microwave entity.</param>
    private void RollMalfunction(Entity<ActiveMicrowaveComponent, MicrowaveComponent> ent)
    {
        var active = ent.Comp1;
        var microwave = ent.Comp2;

        // Are we ready to roll a malfunction yet?
        if (active.MalfunctionTime == TimeSpan.Zero
            || active.MalfunctionTime > _gameTiming.CurTime)
            return;

        active.MalfunctionTime = _gameTiming.CurTime + TimeSpan.FromSeconds(microwave.MalfunctionInterval);

        if (_random.Prob(microwave.ExplosionChance))
        {
            Explode((ent, microwave));
            return;
        }

        if (_random.Prob(microwave.LightningChance))
            _lightning.ShootRandomLightnings(ent, 1.0f, 2, microwave.MalfunctionSpark, triggerLightningEvents: false);
    }
}
