using Content.Server.Body.Systems;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Construction;
using Content.Server.Explosion.EntitySystems;
using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Kitchen.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
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

namespace Content.Server.Kitchen.EntitySystems
{
    public sealed class MicrowaveSystem : EntitySystem
    {
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly PowerReceiverSystem _power = default!;
        [Dependency] private readonly RecipeManager _recipeManager = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly LightningSystem _lightning = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly ExplosionSystem _explosion = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly TemperatureSystem _temperature = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
        [Dependency] private readonly HandsSystem _handsSystem = default!;
        [Dependency] private readonly SharedItemSystem _item = default!;

        [ValidatePrototypeId<EntityPrototype>]
        private const string MalfunctionSpark = "Spark";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MicrowaveComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<MicrowaveComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<MicrowaveComponent, SolutionContainerChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<MicrowaveComponent, EntInsertedIntoContainerMessage>(OnContentUpdate);
            SubscribeLocalEvent<MicrowaveComponent, EntRemovedFromContainerMessage>(OnContentUpdate);
            SubscribeLocalEvent<MicrowaveComponent, InteractUsingEvent>(OnInteractUsing, after: new[] { typeof(AnchorableSystem) });
            SubscribeLocalEvent<MicrowaveComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<MicrowaveComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<MicrowaveComponent, AnchorStateChangedEvent>(OnAnchorChanged);
            SubscribeLocalEvent<MicrowaveComponent, SuicideEvent>(OnSuicide);

            SubscribeLocalEvent<MicrowaveComponent, SignalReceivedEvent>(OnSignalReceived);

            SubscribeLocalEvent<MicrowaveComponent, MicrowaveStartCookMessage>((u, c, m) => Wzhzhzh(u, c, m.Session.AttachedEntity));
            SubscribeLocalEvent<MicrowaveComponent, MicrowaveEjectMessage>(OnEjectMessage);
            SubscribeLocalEvent<MicrowaveComponent, MicrowaveEjectSolidIndexedMessage>(OnEjectIndex);
            SubscribeLocalEvent<MicrowaveComponent, MicrowaveSelectCookTimeMessage>(OnSelectTime);

            SubscribeLocalEvent<ActiveMicrowaveComponent, ComponentStartup>(OnCookStart);
            SubscribeLocalEvent<ActiveMicrowaveComponent, ComponentShutdown>(OnCookStop);
            SubscribeLocalEvent<ActiveMicrowaveComponent, EntInsertedIntoContainerMessage>(OnActiveMicrowaveInsert);
            SubscribeLocalEvent<ActiveMicrowaveComponent, EntRemovedFromContainerMessage>(OnActiveMicrowaveRemove);

            SubscribeLocalEvent<ActivelyMicrowavedComponent, OnConstructionTemperatureEvent>(OnConstructionTemp);
        }

        private void OnCookStart(Entity<ActiveMicrowaveComponent> ent, ref ComponentStartup args)
        {
            if (!TryComp<MicrowaveComponent>(ent, out var microwaveComponent))
                return;
            SetAppearance(ent.Owner, MicrowaveVisualState.Cooking, microwaveComponent);

            microwaveComponent.PlayingStream =
                _audio.PlayPvs(microwaveComponent.LoopingSound, ent, AudioParams.Default.WithLoop(true).WithMaxDistance(5)).Value.Entity;
        }

        private void OnCookStop(Entity<ActiveMicrowaveComponent> ent, ref ComponentShutdown args)
        {
            if (!TryComp<MicrowaveComponent>(ent, out var microwaveComponent))
                return;

            SetAppearance(ent.Owner, MicrowaveVisualState.Idle, microwaveComponent);
            microwaveComponent.PlayingStream = _audio.Stop(microwaveComponent.PlayingStream);
        }

        private void OnActiveMicrowaveInsert(Entity<ActiveMicrowaveComponent> ent, ref EntInsertedIntoContainerMessage args)
        {
            AddComp<ActivelyMicrowavedComponent>(args.Entity);
        }

        private void OnActiveMicrowaveRemove(Entity<ActiveMicrowaveComponent> ent, ref EntRemovedFromContainerMessage args)
        {
            EntityManager.RemoveComponentDeferred<ActivelyMicrowavedComponent>(args.Entity);
        }

        private void OnConstructionTemp(Entity<ActivelyMicrowavedComponent> ent, ref OnConstructionTemperatureEvent args)
        {
            args.Result = HandleResult.False;
            return;
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

        private void SubtractContents(MicrowaveComponent component, FoodRecipePrototype recipe)
        {
            // TODO Turn recipe.IngredientsReagents into a ReagentQuantity[]

            var totalReagentsToRemove = new Dictionary<string, FixedPoint2>(recipe.IngredientsReagents);

            // this is spaghetti ngl
            foreach (var item in component.Storage.ContainedEntities)
            {
                if (!TryComp<SolutionContainerManagerComponent>(item, out var solMan))
                    continue;

                // go over every solution
                foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((item, solMan)))
                {
                    var solution = soln.Comp.Solution;
                    foreach (var (reagent, _) in recipe.IngredientsReagents)
                    {
                        // removed everything
                        if (!totalReagentsToRemove.ContainsKey(reagent))
                            continue;

                        var quant = solution.GetTotalPrototypeQuantity(reagent);

                        if (quant >= totalReagentsToRemove[reagent])
                        {
                            quant = totalReagentsToRemove[reagent];
                            totalReagentsToRemove.Remove(reagent);
                        }
                        else
                        {
                            totalReagentsToRemove[reagent] -= quant;
                        }

                        _solutionContainer.RemoveReagent(soln, reagent, quant);
                    }
                }
            }

            foreach (var recipeSolid in recipe.IngredientsSolids)
            {
                for (var i = 0; i < recipeSolid.Value; i++)
                {
                    foreach (var item in component.Storage.ContainedEntities)
                    {
                        var metaData = MetaData(item);
                        if (metaData.EntityPrototype == null)
                        {
                            continue;
                        }

                        if (metaData.EntityPrototype.ID == recipeSolid.Key)
                        {
                            _container.Remove(item, component.Storage);
                            EntityManager.DeleteEntity(item);
                            break;
                        }
                    }
                }
            }
        }

        private void OnInit(Entity<MicrowaveComponent> ent, ref ComponentInit args)
        {
            // this really does have to be in ComponentInit
            ent.Comp.Storage = _container.EnsureContainer<Container>(ent, "microwave_entity_container");
        }

        private void OnMapInit(Entity<MicrowaveComponent> ent, ref MapInitEvent args)
        {
            _deviceLink.EnsureSinkPorts(ent, ent.Comp.OnPort);
        }

        private void OnSuicide(Entity<MicrowaveComponent> ent, ref SuicideEvent args)
        {
            if (args.Handled)
                return;

            args.SetHandled(SuicideKind.Heat);
            var victim = args.Victim;
            var headCount = 0;

            if (TryComp<BodyComponent>(victim, out var body))
            {
                var headSlots = _bodySystem.GetBodyChildrenOfType(victim, BodyPartType.Head, body);

                foreach (var part in headSlots)
                {
                    _container.Insert(part.Id, ent.Comp.Storage);
                    headCount++;
                }
            }

            var othersMessage = headCount > 1
                ? Loc.GetString("microwave-component-suicide-multi-head-others-message", ("victim", victim))
                : Loc.GetString("microwave-component-suicide-others-message", ("victim", victim));

            var selfMessage = headCount > 1
                ? Loc.GetString("microwave-component-suicide-multi-head-message")
                : Loc.GetString("microwave-component-suicide-message");

            _popupSystem.PopupEntity(othersMessage, victim, Filter.PvsExcept(victim), true);
            _popupSystem.PopupEntity(selfMessage, victim, victim);

            _audio.PlayPvs(ent.Comp.ClickSound, ent.Owner, AudioParams.Default.WithVolume(-2));
            ent.Comp.CurrentCookTimerTime = 10;
            Wzhzhzh(ent.Owner, ent.Comp, args.Victim);
            UpdateUserInterfaceState(ent.Owner, ent.Comp);
        }

        private void OnSolutionChange(Entity<MicrowaveComponent> ent, ref SolutionContainerChangedEvent args)
        {
            UpdateUserInterfaceState(ent, ent.Comp);
        }

        private void OnContentUpdate(EntityUid uid, MicrowaveComponent component, ContainerModifiedMessage args) // For some reason ContainerModifiedMessage just can't be used at all with Entity<T>. TODO: replace with Entity<T> syntax once that's possible
        {
            UpdateUserInterfaceState(uid, component);
        }

        private void OnInteractUsing(Entity<MicrowaveComponent> ent, ref InteractUsingEvent args)
        {
            if (args.Handled)
                return;
            if (!(TryComp<ApcPowerReceiverComponent>(ent, out var apc) && apc.Powered))
            {
                _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-no-power"), ent, args.User);
                return;
            }

            if (ent.Comp.Broken)
            {
                _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-broken"), ent, args.User);
                return;
            }

            if (TryComp<ItemComponent>(args.Used, out var item))
            {
                // check if size of an item you're trying to put in is too big
                if (_item.GetSizePrototype(item.Size) > _item.GetSizePrototype(ent.Comp.MaxItemSize))
                {
                    _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-item-too-big", ("item", args.Used)), ent, args.User);
                    return;
                }
            }
            else
            {
                // check if thing you're trying to put in isn't an item
                _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-using-transfer-fail"), ent, args.User);
                return;
            }

            if (ent.Comp.Storage.Count >= ent.Comp.Capacity)
            {
                _popupSystem.PopupEntity(Loc.GetString("microwave-component-interact-full"), ent, args.User);
                return;
            }

            args.Handled = true;
            _handsSystem.TryDropIntoContainer(args.User, args.Used, ent.Comp.Storage);
            UpdateUserInterfaceState(ent, ent.Comp);
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

        public void UpdateUserInterfaceState(EntityUid uid, MicrowaveComponent component)
        {
            var ui = _userInterface.GetUiOrNull(uid, MicrowaveUiKey.Key);
            if (ui == null)
                return;

            _userInterface.SetUiState(ui, new MicrowaveUpdateUserInterfaceState(
                GetNetEntityArray(component.Storage.ContainedEntities.ToArray()),
                HasComp<ActiveMicrowaveComponent>(uid),
                component.CurrentCookTimeButtonIndex,
                component.CurrentCookTimerTime,
                component.CurrentCookTimeEnd
            ));
        }

        public void SetAppearance(EntityUid uid, MicrowaveVisualState state, MicrowaveComponent? component = null, AppearanceComponent? appearanceComponent = null)
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
                _explosion.TriggerExplosive(ent);
                return;  // microwave is fucked, stop the cooking.
            }

            if (_random.Prob(ent.Comp2.LightningChance))
                _lightning.ShootRandomLightnings(ent, 1.0f, 2, MalfunctionSpark, triggerLightningEvents: false);
        }

        /// <summary>
        /// Starts Cooking
        /// </summary>
        /// <remarks>
        /// It does not make a "wzhzhzh" sound, it makes a "mmmmmmmm" sound!
        /// -emo
        /// </remarks>
        public void Wzhzhzh(EntityUid uid, MicrowaveComponent component, EntityUid? user)
        {
            if (!HasContents(component) || HasComp<ActiveMicrowaveComponent>(uid) || !(TryComp<ApcPowerReceiverComponent>(uid, out var apc) && apc.Powered))
                return;

            var solidsDict = new Dictionary<string, int>();
            var reagentDict = new Dictionary<string, FixedPoint2>();
            var malfunctioning = false;
            // TODO use lists of Reagent quantities instead of reagent prototype ids.
            foreach (var item in component.Storage.ContainedEntities.ToArray())
            {
                // special behavior when being microwaved ;)
                var ev = new BeingMicrowavedEvent(uid, user);
                RaiseLocalEvent(item, ev);

                if (ev.Handled)
                {
                    UpdateUserInterfaceState(uid, component);
                    return;
                }

                if (_tag.HasTag(item, "Metal"))
                {
                    malfunctioning = true;
                }

                if (_tag.HasTag(item, "Plastic"))
                {
                    var junk = Spawn(component.BadRecipeEntityId, Transform(uid).Coordinates);
                    _container.Insert(junk, component.Storage);
                    Del(item);
                    continue;
                }

                AddComp<ActivelyMicrowavedComponent>(item);

                var metaData = MetaData(item); //this simply begs for cooking refactor
                if (metaData.EntityPrototype == null)
                    continue;

                if (solidsDict.ContainsKey(metaData.EntityPrototype.ID))
                {
                    solidsDict[metaData.EntityPrototype.ID]++;
                }
                else
                {
                    solidsDict.Add(metaData.EntityPrototype.ID, 1);
                }

                if (!TryComp<SolutionContainerManagerComponent>(item, out var solMan))
                    continue;

                foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((item, solMan)))
                {
                    var solution = soln.Comp.Solution;
                    foreach (var (reagent, quantity) in solution.Contents)
                    {
                        if (reagentDict.ContainsKey(reagent.Prototype))
                            reagentDict[reagent.Prototype] += quantity;
                        else
                            reagentDict.Add(reagent.Prototype, quantity);
                    }
                }
            }

            // Check recipes
            var portionedRecipe = _recipeManager.Recipes.Select(r =>
                CanSatisfyRecipe(component, r, solidsDict, reagentDict)).FirstOrDefault(r => r.Item2 > 0);

            _audio.PlayPvs(component.StartCookingSound, uid);
            var activeComp = AddComp<ActiveMicrowaveComponent>(uid); //microwave is now cooking
            activeComp.CookTimeRemaining = component.CurrentCookTimerTime * component.CookTimeMultiplier;
            activeComp.TotalTime = component.CurrentCookTimerTime; //this doesn't scale so that we can have the "actual" time
            activeComp.PortionedRecipe = portionedRecipe;
            component.CurrentCookTimeEnd = _gameTiming.CurTime + TimeSpan.FromSeconds(component.CurrentCookTimerTime);
            if (malfunctioning)
                activeComp.MalfunctionTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.MalfunctionInterval);
            UpdateUserInterfaceState(uid, component);
        }

        private void StopCooking(Entity<MicrowaveComponent> ent)
        {
            RemCompDeferred<ActiveMicrowaveComponent>(ent);
            foreach (var solid in ent.Comp.Storage.ContainedEntities)
            {
                RemCompDeferred<ActivelyMicrowavedComponent>(solid);
            }
        }

        public static (FoodRecipePrototype, int) CanSatisfyRecipe(MicrowaveComponent component, FoodRecipePrototype recipe, Dictionary<string, int> solids, Dictionary<string, FixedPoint2> reagents)
        {
            var portions = 0;

            if (component.CurrentCookTimerTime % recipe.CookTime != 0)
            {
                //can't be a multiple of this recipe
                return (recipe, 0);
            }

            foreach (var solid in recipe.IngredientsSolids)
            {
                if (!solids.ContainsKey(solid.Key))
                    return (recipe, 0);

                if (solids[solid.Key] < solid.Value)
                    return (recipe, 0);

                portions = portions == 0
                    ? solids[solid.Key] / solid.Value.Int()
                    : Math.Min(portions, solids[solid.Key] / solid.Value.Int());
            }

            foreach (var reagent in recipe.IngredientsReagents)
            {
                // TODO Turn recipe.IngredientsReagents into a ReagentQuantity[]
                if (!reagents.ContainsKey(reagent.Key))
                    return (recipe, 0);

                if (reagents[reagent.Key] < reagent.Value)
                    return (recipe, 0);

                portions = portions == 0
                    ? reagents[reagent.Key].Int() / reagent.Value.Int()
                    : Math.Min(portions, reagents[reagent.Key].Int() / reagent.Value.Int());
            }

            //cook only as many of those portions as time allows
            return (recipe, (int) Math.Min(portions, component.CurrentCookTimerTime / recipe.CookTime));
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

        #region ui
        private void OnEjectMessage(Entity<MicrowaveComponent> ent, ref MicrowaveEjectMessage args)
        {
            if (!HasContents(ent.Comp) || HasComp<ActiveMicrowaveComponent>(ent))
                return;

            _container.EmptyContainer(ent.Comp.Storage);
            _audio.PlayPvs(ent.Comp.ClickSound, ent, AudioParams.Default.WithVolume(-2));
            UpdateUserInterfaceState(ent, ent.Comp);
        }

        private void OnEjectIndex(Entity<MicrowaveComponent> ent, ref MicrowaveEjectSolidIndexedMessage args)
        {
            if (!HasContents(ent.Comp) || HasComp<ActiveMicrowaveComponent>(ent))
                return;

            _container.Remove(EntityManager.GetEntity(args.EntityID), ent.Comp.Storage);
            UpdateUserInterfaceState(ent, ent.Comp);
        }

        private void OnSelectTime(Entity<MicrowaveComponent> ent, ref MicrowaveSelectCookTimeMessage args)
        {
            if (!HasContents(ent.Comp) || HasComp<ActiveMicrowaveComponent>(ent) || !(TryComp<ApcPowerReceiverComponent>(ent, out var apc) && apc.Powered))
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
        #endregion
    }
}
