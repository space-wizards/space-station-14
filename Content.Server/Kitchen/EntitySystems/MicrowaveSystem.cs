using Content.Server.Administration.Logs;
using Content.Server.Construction;
using Content.Server.Explosion.EntitySystems;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Kitchen.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Temperature.Systems;
using Content.Shared.Chemistry.Components.SolutionManager;
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
using Content.Shared.Tag;
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
using Content.Shared.Temperature.Components;
using Content.Shared.Kitchen.EntitySystems;
using Content.Shared.Whitelist;

namespace Content.Server.Kitchen.EntitySystems;

public sealed partial class MicrowaveSystem : SharedMicrowaveSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPowerStateSystem _powerState = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RecipeManager _recipeManager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedSuicideSystem _suicide = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private static readonly EntProtoId MalfunctionSpark = "Spark";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MicrowaveComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MicrowaveComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MicrowaveComponent, SolutionContainerChangedEvent>(OnSolutionChange);
        SubscribeLocalEvent<MicrowaveComponent, EntInsertedIntoContainerMessage>(OnContentUpdate);
        SubscribeLocalEvent<MicrowaveComponent, EntRemovedFromContainerMessage>(OnContentUpdate);
        SubscribeLocalEvent<MicrowaveComponent, InteractUsingEvent>(OnInteractUsing, after: new[] { typeof(AnchorableSystem) });
        SubscribeLocalEvent<MicrowaveComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<MicrowaveComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<MicrowaveComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<MicrowaveComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<MicrowaveComponent, SuicideByEnvironmentEvent>(OnSuicideByEnvironment);

        SubscribeLocalEvent<MicrowaveComponent, SignalReceivedEvent>(OnSignalReceived);

        SubscribeLocalEvent<MicrowaveComponent, MicrowaveStartCookMessage>((u, c, m) => Wzhzhzh(u, c, m.Actor));
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

    private void OnCookStart(Entity<ActiveMicrowaveComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<MicrowaveComponent>(ent, out var microwaveComponent))
            return;
        SetAppearance(ent.Owner, MicrowaveVisualState.Cooking, microwaveComponent);

        microwaveComponent.PlayingStream =
            _audio.PlayPvs(microwaveComponent.LoopingSound, ent, AudioParams.Default.WithLoop(true).WithMaxDistance(5))?.Entity;
        _powerState.SetWorkingState(ent.Owner, true);
    }

    private void OnCookStop(Entity<ActiveMicrowaveComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<MicrowaveComponent>(ent, out var microwaveComponent))
            return;

        SetAppearance(ent.Owner, MicrowaveVisualState.Idle, microwaveComponent);
        microwaveComponent.PlayingStream = _audio.Stop(microwaveComponent.PlayingStream);
        _powerState.SetWorkingState(ent.Owner, false);
    }

    // Stop items from transforming through constructiongraphs while being microwaved.
    // They might be reserved for a microwave recipe.
    private void OnConstructionTemp(Entity<ActivelyMicrowavedComponent> ent, ref OnConstructionTemperatureEvent args)
    {
        args.Result = HandleResult.False;
    }

    // Stop reagents from reacting if they are currently reserved for a microwave recipe.
    // For example Egg would cook into EggCooked, causing it to not being removed once we are done microwaving.
    private void OnReactionAttempt(Entity<ActivelyMicrowavedComponent> ent, ref SolutionRelayEvent<ReactionAttemptEvent> args)
    {
        if (!TryComp<ActiveMicrowaveComponent>(ent.Comp.Microwave, out var activeMicrowaveComp))
            return;

        if (activeMicrowaveComp.PortionedRecipe.Item1 == null) // no recipe selected
            return;

        var recipeReagents = activeMicrowaveComp.PortionedRecipe.Item1.Ingredients.Reagents.Keys;

        foreach (var reagent in recipeReagents)
        {
            if (args.Event.Reaction.Reactants.ContainsKey(reagent))
            {
                args.Event.Cancelled = true;
                return;
            }
        }
    }

    /// <summary>
    ///     Adds temperature to every item in the microwave,
    ///     based on the time it took to microwave.
    /// </summary>
    /// <param name="component">The microwave that is heating up.</param>
    /// <param name="time">The time on the microwave, in seconds.</param>
    private void AddTemperature(MicrowaveComponent component, float time)
    {
        var heatToAdd = time * component.BaseHeatMultiplier;
        foreach (var entity in component.Storage.ContainedEntities)
        {
            if (TryComp<TemperatureComponent>(entity, out var tempComp))
                _temperature.ChangeHeat(entity, heatToAdd * component.ObjectHeatMultiplier, false, tempComp);

            if (!TryComp<SolutionContainerManagerComponent>(entity, out var solutions))
                continue;
            foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((entity, solutions)))
            {
                var solution = soln.Comp.Solution;
                if (solution.Temperature > component.TemperatureUpperThreshold)
                    continue;

                _solutionContainer.AddThermalEnergy(soln, heatToAdd);
            }
        }
    }

    private void OnInit(Entity<MicrowaveComponent> ent, ref ComponentInit args)
    {
        // this really does have to be in ComponentInit
        ent.Comp.Storage = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
    }

    private void OnMapInit(Entity<MicrowaveComponent> ent, ref MapInitEvent args)
    {
        _deviceLink.EnsureSinkPorts(ent, ent.Comp.OnPort);
    }

    /// <summary>
    /// Kills the user by microwaving their head
    /// TODO: Make this not awful, it keeps any items attached to your head still on and you can revive someone and cogni them so you have some dumb headless fuck running around. I've seen it happen.
    /// </summary>
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
        Wzhzhzh(ent.Owner, ent.Comp, args.Victim);
        UpdateUserInterfaceState(ent.Owner, ent.Comp);
        args.Handled = true;
    }

    private void OnBreak(Entity<MicrowaveComponent> ent, ref BreakageEventArgs args)
    {
        ent.Comp.Broken = true;
        SetAppearance(ent, MicrowaveVisualState.Broken, ent.Comp);
        StopCooking(ent);
        _container.EmptyContainer(ent.Comp.Storage);
        UpdateUserInterfaceState(ent, ent.Comp);
    }

    private void OnPowerChanged(Entity<MicrowaveComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            SetAppearance(ent, MicrowaveVisualState.Idle, ent.Comp);
            StopCooking(ent);
        }
        UpdateUserInterfaceState(ent, ent.Comp);
    }

    private void OnAnchorChanged(EntityUid uid, MicrowaveComponent component, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            _container.EmptyContainer(component.Storage);
    }

    private void OnSignalReceived(Entity<MicrowaveComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != ent.Comp.OnPort)
            return;

        if (ent.Comp.Broken || !_power.IsPowered(ent))
            return;

        Wzhzhzh(ent.Owner, ent.Comp, null);
    }

    public void UpdateUserInterfaceState(Entity<MicrowaveComponent> microwave)
    {
        var uid = microwave.Owner;
        var component = microwave.Comp;
        var containedItems = GetNetEntityArray(component.Storage.ContainedEntities.ToArray());
        var isActive = HasComp<ActiveMicrowaveComponent>(uid);
        var state = new MicrowaveUpdateUserInterfaceState(
            containedItems,
            isActive,
            component.CurrentCookTimeButtonIndex,
            component.CurrentCookTimerTime,
            component.CurrentCookTimeEnd);

        _userInterface.SetUiState(uid, MicrowaveUiKey.Key, state);
    }

    public void UpdateUserInterfaceState(EntityUid uid, MicrowaveComponent component)
    {
        UpdateUserInterfaceState((uid, component));
    }

    public void SetAppearance(EntityUid uid,
        MicrowaveVisualState state,
        MicrowaveComponent? component = null,
        AppearanceComponent? appearanceComponent = null)
    {
        if (!Resolve(uid, ref component, ref appearanceComponent, false))
            return;
        var display = component.Broken ? MicrowaveVisualState.Broken : state;
        _appearance.SetData(uid, PowerDeviceVisuals.VisualState, display, appearanceComponent);
    }

    public static bool HasContents(MicrowaveComponent component)
    {
        return component.Storage.ContainedEntities.Any();
    }

    /// <summary>
    /// Explodes the microwave internally, turning it into a broken state, destroying its board, and spitting out its machine parts
    /// </summary>
    /// <param name="ent"></param>
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
    /// Handles the attempted cooking of unsafe objects
    /// </summary>
    /// <remarks>
    /// Returns false if the microwave didn't explode, true if it exploded.
    /// </remarks>
    private void RollMalfunction(Entity<ActiveMicrowaveComponent, MicrowaveComponent> ent)
    {
        if (ent.Comp1.MalfunctionTime == TimeSpan.Zero)
            return;

        if (ent.Comp1.MalfunctionTime > _gameTiming.CurTime)
            return;

        ent.Comp1.MalfunctionTime = _gameTiming.CurTime + TimeSpan.FromSeconds(ent.Comp2.MalfunctionInterval);
        if (_random.Prob(ent.Comp2.ExplosionChance))
        {
            Explode((ent, ent.Comp2));
            return;  // microwave is fucked, stop the cooking.
        }

        if (_random.Prob(ent.Comp2.LightningChance))
            _lightning.ShootRandomLightnings(ent, 1.0f, 2, MalfunctionSpark, triggerLightningEvents: false);
    }

    private void StopCooking(Entity<MicrowaveComponent> ent)
    {
        RemCompDeferred<ActiveMicrowaveComponent>(ent);
        foreach (var solid in ent.Comp.Storage.ContainedEntities)
        {
            RemCompDeferred<ActivelyMicrowavedComponent>(solid);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveMicrowaveComponent, MicrowaveComponent>();
        while (query.MoveNext(out var uid, out var active, out var microwave))
        {

            active.CookTimeRemaining -= frameTime;

            RollMalfunction((uid, active, microwave));

            //check if there's still cook time left
            if (active.CookTimeRemaining > 0)
            {
                AddTemperature(microwave, frameTime);
                continue;
            }

            //this means the microwave has finished cooking.
            AddTemperature(microwave, Math.Max(frameTime + active.CookTimeRemaining, 0)); //Though there's still a little bit more heat to pump out

            if (active.PortionedRecipe.Item1 != null)
            {
                var coords = Transform(uid).Coordinates;
                for (var i = 0; i < active.PortionedRecipe.Item2; i++)
                {
                    SubtractContents(microwave, active.PortionedRecipe.Item1);
                    Spawn(active.PortionedRecipe.Item1.Result, coords);
                }
            }

            _container.EmptyContainer(microwave.Storage);
            microwave.CurrentCookTimeEnd = TimeSpan.Zero;
            UpdateUserInterfaceState(uid, microwave);
            _audio.PlayPvs(microwave.FoodDoneSound, uid);
            StopCooking((uid, microwave));
        }
    }

    private void OnSelectTime(Entity<MicrowaveComponent> ent, ref MicrowaveSelectCookTimeMessage args)
    {
        if (!HasContents(ent.Comp) || HasComp<ActiveMicrowaveComponent>(ent) || _power.IsPowered(ent.Owner))
            return;

        // some validation to prevent trollage
        if (args.NewCookTime % 5 != 0 || args.NewCookTime > ent.Comp.MaxCookTime)
            return;

        ent.Comp.CurrentCookTimeButtonIndex = args.ButtonIndex;
        ent.Comp.CurrentCookTimerTime = args.NewCookTime;
        ent.Comp.CurrentCookTimeEnd = TimeSpan.Zero;
        _audio.PlayPvs(ent.Comp.ClickSound, ent, AudioParams.Default.WithVolume(-2));
        UpdateUserInterfaceState(ent, ent.Comp);
    }
}
