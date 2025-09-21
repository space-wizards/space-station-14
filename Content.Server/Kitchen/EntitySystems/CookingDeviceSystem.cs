using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.Construction;
using Content.Server.Explosion.EntitySystems;
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
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Database;
using Content.Shared.DeviceLinking.Events;
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
using Content.Shared.Stacks;
using Content.Server.Construction.Components;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Robust.Shared.Utility;

namespace Content.Server.Kitchen.EntitySystems
{
    public sealed class CookingDeviceSystem : EntitySystem // Starlight-edit: renamed from MicrowaveSystem to CookingDeviceSystem
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
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly TemperatureSystem _temperature = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
        [Dependency] private readonly HandsSystem _handsSystem = default!;
        [Dependency] private readonly SharedItemSystem _item = default!;
        [Dependency] private readonly SharedStackSystem _stack = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedSuicideSystem _suicide = default!;

        private static readonly EntProtoId MalfunctionSpark = "Spark";

        private static readonly ProtoId<TagPrototype> MetalTag = "Metal";
        private static readonly ProtoId<TagPrototype> PlasticTag = "Plastic";

        public override void Initialize()
        {
            base.Initialize();
            
            // Starlight-start: renamed from MicrowaveComponent to CookingDeviceComponent and ActiveMicrowaveComponent to ActiveCookingDeviceComponent
            SubscribeLocalEvent<CookingDeviceComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<CookingDeviceComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<CookingDeviceComponent, SolutionContainerChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<CookingDeviceComponent, EntInsertedIntoContainerMessage>(OnContentUpdate);
            SubscribeLocalEvent<CookingDeviceComponent, EntRemovedFromContainerMessage>(OnContentUpdate);
            SubscribeLocalEvent<CookingDeviceComponent, InteractUsingEvent>(OnInteractUsing, after: new[] { typeof(AnchorableSystem) });
            SubscribeLocalEvent<CookingDeviceComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
            SubscribeLocalEvent<CookingDeviceComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<CookingDeviceComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<CookingDeviceComponent, AnchorStateChangedEvent>(OnAnchorChanged);
            SubscribeLocalEvent<CookingDeviceComponent, SuicideByEnvironmentEvent>(OnSuicideByEnvironment);

            SubscribeLocalEvent<CookingDeviceComponent, SignalReceivedEvent>(OnSignalReceived);

            SubscribeLocalEvent<CookingDeviceComponent, MicrowaveStartCookMessage>((u, c, m) => Wzhzhzh(u, c, m.Actor));
            SubscribeLocalEvent<CookingDeviceComponent, MicrowaveStopCookMessage>(OnStopMessage);
            SubscribeLocalEvent<CookingDeviceComponent, MicrowaveEjectMessage>(OnEjectMessage);
            SubscribeLocalEvent<CookingDeviceComponent, MicrowaveEjectSolidIndexedMessage>(OnEjectIndex);
            SubscribeLocalEvent<CookingDeviceComponent, MicrowaveSelectCookTimeMessage>(OnSelectTime);

            SubscribeLocalEvent<ActiveCookingDeviceComponent, ComponentStartup>(OnCookStart);
            SubscribeLocalEvent<ActiveCookingDeviceComponent, ComponentShutdown>(OnCookStop);
            SubscribeLocalEvent<ActiveCookingDeviceComponent, EntInsertedIntoContainerMessage>(OnActiveMicrowaveInsert);
            SubscribeLocalEvent<ActiveCookingDeviceComponent, EntRemovedFromContainerMessage>(OnActiveMicrowaveRemove);

            SubscribeLocalEvent<ActivelyCookedComponent, OnConstructionTemperatureEvent>(OnConstructionTemp);
            SubscribeLocalEvent<ActivelyCookedComponent, SolutionRelayEvent<ReactionAttemptEvent>>(OnReactionAttempt);
            // Starlight-end

            SubscribeLocalEvent<FoodRecipeProviderComponent, GetSecretRecipesEvent>(OnGetSecretRecipes);
            
            // Starlight-start
            SubscribeLocalEvent<CookingDeviceComponent, BoundUIOpenedEvent>(OnBuiOpened);
            SubscribeLocalEvent<CookingDeviceComponent, BoundUIClosedEvent>(OnBuiClosed);
            // Starlight-end
            
        }
        
        // Starlight-start
        private void OnBuiOpened(EntityUid uid, CookingDeviceComponent component, BoundUIOpenedEvent args) => SetAppearance(uid, null, component, Opened: true);
        
        private void OnBuiClosed(EntityUid uid, CookingDeviceComponent component, BoundUIClosedEvent args) => SetAppearance(uid, null, component, Opened: false);
        // Starlight-end

        private void OnCookStart(Entity<ActiveCookingDeviceComponent> ent, ref ComponentStartup args) // Starlight-edit
        {
            if (!TryComp<CookingDeviceComponent>(ent, out var CookingDeviceComponent)) // Starlight-edit
                return;
            SetAppearance(ent.Owner, MicrowaveVisualState.Cooking, CookingDeviceComponent); // Starlight-edit

            CookingDeviceComponent.PlayingStream = _audio.PlayPvs(CookingDeviceComponent.LoopingSound, ent, AudioParams.Default.WithLoop(true).WithMaxDistance(5))?.Entity; // Starlight-edit
        }

        private void OnCookStop(Entity<ActiveCookingDeviceComponent> ent, ref ComponentShutdown args) // Starlight-edit
        {
            if (!TryComp<CookingDeviceComponent>(ent, out var CookingDeviceComponent)) // Starlight-edit
                return;
            
            // Starlight-start
            SetAppearance(ent.Owner, MicrowaveVisualState.Idle, CookingDeviceComponent);
            CookingDeviceComponent.PlayingStream = _audio.Stop(CookingDeviceComponent.PlayingStream);
            CookingDeviceComponent.StartedCookTime = TimeSpan.Zero;
            UpdateUserInterfaceState(ent.Owner, CookingDeviceComponent, false);
            // Starlight-end
        }

        private void OnActiveMicrowaveInsert(Entity<ActiveCookingDeviceComponent> ent, ref EntInsertedIntoContainerMessage args) // Starlight-edit
        {
            var microwavedComp = AddComp<ActivelyCookedComponent>(args.Entity); // Starlight-edit
            microwavedComp.Microwave = ent.Owner;
        }

        private void OnActiveMicrowaveRemove(Entity<ActiveCookingDeviceComponent> ent, ref EntRemovedFromContainerMessage args)
        {
            RemCompDeferred<ActivelyCookedComponent>(args.Entity);
        }

        // Stop items from transforming through constructiongraphs while being microwaved.
        // They might be reserved for a microwave recipe.
        private void OnConstructionTemp(Entity<ActivelyCookedComponent> ent, ref OnConstructionTemperatureEvent args) => args.Result = HandleResult.False; // Starlight-edit

        // Stop reagents from reacting if they are currently reserved for a microwave recipe.
        // For example Egg would cook into EggCooked, causing it to not being removed once we are done microwaving.
        private void OnReactionAttempt(Entity<ActivelyCookedComponent> ent, ref SolutionRelayEvent<ReactionAttemptEvent> args) // Starlight-edit
        {
            if (!TryComp<ActiveCookingDeviceComponent>(ent.Comp.Microwave, out var activeMicrowaveComp)) // Starlight-edit
                return;

            if (activeMicrowaveComp.PortionedRecipes.Count == 0) // Starlight-edit, no recipe selected
                return;

            // Starlight-start

            foreach (var (recipe, availableAmount) in activeMicrowaveComp.PortionedRecipes)
            {
                var recipeReagents = recipe.IngredientsReagents.Keys;

                foreach (var reagent in recipeReagents)
                {
                    if (args.Event.Reaction.Reactants.ContainsKey(reagent))
                    {
                        args.Event.Cancelled = true;
                        return;
                    }
                }
            }
            
            // Starlight-end
        }

        /// <summary>
        ///     Adds temperature to every item in the microwave,
        ///     based on the time it took to microwave.
        /// </summary>
        /// <param name="component">The microwave that is heating up.</param>
        /// <param name="time">The time on the microwave, in seconds.</param>
        private void AddTemperature(CookingDeviceComponent component, float time) // Starlight-edit
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

        private bool SubtractContents(CookingDeviceComponent component, FoodRecipePrototype recipe) // Starlight-edit
        {
            // TODO Turn recipe.IngredientsReagents into a ReagentQuantity[]

            var totalReagentsToRemove = new Dictionary<string, FixedPoint2>(recipe.IngredientsReagents);

            // Starlight-start: Check for subsract ability
            foreach (var (reagent, required) in recipe.IngredientsReagents)
            {
                var available = FixedPoint2.Zero;

                foreach (var item in component.Storage.ContainedEntities)
                {
                    if (!_solutionContainer.TryGetDrainableSolution(item, out _, out var solution))
                        continue;

                    available += solution.GetTotalPrototypeQuantity(reagent);
                }

                if (available < required)
                    return false;
            }

            foreach (var recipeSolid in recipe.IngredientsSolids)
            {
                var available = 0;

                foreach (var item in component.Storage.ContainedEntities)
                {
                    string? itemID = null;

                    if (TryComp<StackComponent>(item, out var stackComp))
                        itemID = _prototype.Index<StackPrototype>(stackComp.StackTypeId).Spawn;
                    else
                    {
                        var metaData = MetaData(item);
                        if (metaData.EntityPrototype == null)
                            continue;
                        itemID = metaData.EntityPrototype.ID;
                    }

                    if (itemID == recipeSolid.Key)
                    {
                        available += stackComp?.Count ?? 1;
                    }
                }

                if (available < recipeSolid.Value)
                    return false;
            }
            // Starlight-end

            // this is spaghetti ngl
            foreach (var item in component.Storage.ContainedEntities)
            {
                // use the same reagents as when we selected the recipe
                if (!_solutionContainer.TryGetDrainableSolution(item, out var solutionEntity, out var solution))
                    continue;

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
                        totalReagentsToRemove[reagent] -= quant;

                    _solutionContainer.RemoveReagent(solutionEntity.Value, reagent, quant);
                }
            }

            foreach (var recipeSolid in recipe.IngredientsSolids)
            {
                for (var i = 0; i < recipeSolid.Value; i++)
                {
                    foreach (var item in component.Storage.ContainedEntities)
                    {
                        string? itemID = null;

                        // If an entity has a stack component, use the stacktype instead of prototype id
                        if (TryComp<StackComponent>(item, out var stackComp))
                            itemID = _prototype.Index<StackPrototype>(stackComp.StackTypeId).Spawn;
                        else
                        {
                            var metaData = MetaData(item);
                            if (metaData.EntityPrototype == null)
                                continue;
                            itemID = metaData.EntityPrototype.ID;
                        }

                        if (itemID != recipeSolid.Key)
                            continue;

                        if (stackComp is not null)
                        {
                            if (stackComp.Count == 1)
                                _container.Remove(item, component.Storage);
                            _stack.Use(item, 1, stackComp);
                            break;
                        }
                        else
                        {
                            _container.Remove(item, component.Storage);
                            Del(item);
                            break;
                        }
                    }
                }
            }

            return true; // Starlight-edit: Check for subsract ability
        }

        private void OnInit(Entity<CookingDeviceComponent> ent, ref ComponentInit args) => ent.Comp.Storage = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId); // Starlight-edit: this really does have to be in ComponentInit

        private void OnMapInit(Entity<CookingDeviceComponent> ent, ref MapInitEvent args) => _deviceLink.EnsureSinkPorts(ent, ent.Comp.OnPort); // Starlight-edit

        /// <summary>
        /// Kills the user by microwaving their head
        /// TODO: Make this not awful, it keeps any items attached to your head still on and you can revive someone and cogni them so you have some dumb headless fuck running around. I've seen it happen.
        /// </summary>
        private void OnSuicideByEnvironment(Entity<CookingDeviceComponent> ent, ref SuicideByEnvironmentEvent args) // Starlight-edit
        {
            if (args.Handled)
                return;

            // The act of getting your head microwaved doesn't actually kill you
            if (!TryComp<DamageableComponent>(args.Victim, out var damageableComponent))
                return;

            // The application of lethal damage is what kills you...
            _suicide.ApplyLethalDamage((args.Victim, damageableComponent), "Heat");

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
            args.Handled = true;
        }

        private void OnSolutionChange(Entity<CookingDeviceComponent> ent, ref SolutionContainerChangedEvent args) => UpdateUserInterfaceState(ent, ent.Comp); // Starlight-edit

        private void OnContentUpdate(EntityUid uid, CookingDeviceComponent component, ContainerModifiedMessage args) // Starlight-edit: ContainerModifiedMessage just can't be used at all with Entity<T>, because it's abstract.
        {
            if (component.Storage == args.Container) 
                UpdateUserInterfaceState(uid, component);
        }

        private void OnInsertAttempt(Entity<CookingDeviceComponent> ent, ref ContainerIsInsertingAttemptEvent args) // Starlight-edit
        {
            if (args.Container.ID != ent.Comp.ContainerId)
                return;

            if (ent.Comp.Broken)
            {
                args.Cancel();
                return;
            }

            if (TryComp<ItemComponent>(args.EntityUid, out var item))
            {
                if (_item.GetSizePrototype(item.Size) > _item.GetSizePrototype(ent.Comp.MaxItemSize))
                {
                    args.Cancel();
                    return;
                }
            }
            else
            {
                args.Cancel();
                return;
            }

            if (ent.Comp.Storage.Count >= ent.Comp.Capacity)
                args.Cancel();
        }

        private void OnInteractUsing(Entity<CookingDeviceComponent> ent, ref InteractUsingEvent args) // Starlight-edit
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

        private void OnBreak(Entity<CookingDeviceComponent> ent, ref BreakageEventArgs args) // Starlight-edit
        {
            ent.Comp.Broken = true;
            SetAppearance(ent, MicrowaveVisualState.Broken, ent.Comp);
            StopCooking(ent);
            _container.EmptyContainer(ent.Comp.Storage);
            UpdateUserInterfaceState(ent, ent.Comp);
        }

        private void OnPowerChanged(Entity<CookingDeviceComponent> ent, ref PowerChangedEvent args) // Starlight-edit
        {
            if (!args.Powered)
            {
                SetAppearance(ent, MicrowaveVisualState.Idle, ent.Comp);
                StopCooking(ent);
            }
            UpdateUserInterfaceState(ent, ent.Comp);
        }

        private void OnAnchorChanged(EntityUid uid, CookingDeviceComponent component, ref AnchorStateChangedEvent args) // Starlight-edit
        {
            if (!args.Anchored)
                _container.EmptyContainer(component.Storage);
        }

        private void OnSignalReceived(Entity<CookingDeviceComponent> ent, ref SignalReceivedEvent args) // Starlight-edit
        {
            if (args.Port != ent.Comp.OnPort)
                return;

            if (ent.Comp.Broken || !_power.IsPowered(ent))
                return;

            Wzhzhzh(ent.Owner, ent.Comp, null);
        }

        public void UpdateUserInterfaceState(EntityUid uid, CookingDeviceComponent component, bool? IsBusy = null) // Starlight-edit
        {
            _userInterface.SetUiState(uid, MicrowaveUiKey.Key, new MicrowaveUpdateUserInterfaceState(
                GetNetEntityArray(component.Storage.ContainedEntities.ToArray()),
                IsBusy ?? HasComp<ActiveCookingDeviceComponent>(uid), // Starlight-edit
                component.Safe, // Starlight-edit
                component.CurrentCookTimeButtonIndex,
                component.CurrentCookTimerTime,
                component.CurrentCookTimeEnd, // Starlight-edit
                component.StartedCookTime // Starlight-edit
            ));
        }

        public void SetAppearance(EntityUid uid, MicrowaveVisualState? state = null, CookingDeviceComponent? component = null, AppearanceComponent? appearanceComponent = null, bool? Opened = null) // Starlight-edit
        {
            if (!Resolve(uid, ref component, ref appearanceComponent, false))
                return;
            
            // Starlight-start
            
            if (Opened != null)
            {
                var openedState = Opened.Value ? OpenableKitchenDevice.Opened : OpenableKitchenDevice.Closed;
                _appearance.SetData(uid, PowerDeviceVisuals.VisualState, openedState, appearanceComponent);
            }
            
            if (state == null)
                return;
            
            // Starlight-end
            
            var display = component.Broken ? MicrowaveVisualState.Broken : state;
            _appearance.SetData(uid, PowerDeviceVisuals.VisualState, display, appearanceComponent);
        }

        public static bool HasContents(CookingDeviceComponent component) => component.Storage.ContainedEntities.Any(); // Starlight-edit: I love lambda, so?

        /// <summary>
        /// Explodes the microwave internally, turning it into a broken state, destroying its board, and spitting out its machine parts
        /// </summary>
        /// <param name="ent"></param>
        public void Explode(Entity<CookingDeviceComponent> ent) // Starlight-edit
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
        private void RollMalfunction(Entity<ActiveCookingDeviceComponent, CookingDeviceComponent> ent) // Starlight-edit
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

        /// <summary>
        /// Starts Cooking
        /// </summary>
        /// <remarks>
        /// It does not make a "wzhzhzh" sound, it makes a "mmmmmmmm" sound!
        /// -emo
        /// </remarks>
        public void Wzhzhzh(EntityUid uid, CookingDeviceComponent component, EntityUid? user) // Starlight-edit
        {
            if (!HasContents(component) || HasComp<ActiveCookingDeviceComponent>(uid) || !(TryComp<ApcPowerReceiverComponent>(uid, out var apc) && apc.Powered)) // Starlight-edit
                return;

            var solidsDict = new Dictionary<string, int>();
            var reagentDict = new Dictionary<string, FixedPoint2>();
            var malfunctioning = false;

            int notTrueTypeCount = 0;

            // TODO use lists of Reagent quantities instead of reagent prototype ids.
            foreach (var item in component.Storage.ContainedEntities.ToArray())
            {
                // special behavior when being microwaved ;)
                var ev = new BeingMicrowavedEvent(uid, user);
                RaiseLocalEvent(item, ev);

                // TODO MICROWAVE SPARKS & EFFECTS
                // Various microwaveable entities should probably spawn a spark, play a sound, and generate a pop=up.
                // This should probably be handled by the microwave system, with fields in BeingMicrowavedEvent.

                if (ev.Handled)
                {
                    UpdateUserInterfaceState(uid, component);
                    return;
                }

                if (_tag.HasTag(item, MetalTag))
                    malfunctioning = true;

                if (_tag.HasTag(item, PlasticTag))
                {
                    var junk = Spawn(component.BadRecipeEntityId, Transform(uid).Coordinates);
                    _container.Insert(junk, component.Storage);
                    Del(item);
                    continue;
                }

                var microwavedComp = AddComp<ActivelyCookedComponent>(item); // Starlight-edit
                microwavedComp.Microwave = uid;

                string? solidID = null;
                int amountToAdd = 1;

                // If a microwave recipe uses a stacked item, use the default stack prototype id instead of prototype id
                if (TryComp<StackComponent>(item, out var stackComp))
                {
                    solidID = _prototype.Index<StackPrototype>(stackComp.StackTypeId).Spawn;
                    amountToAdd = stackComp.Count;
                }
                else
                {
                    var metaData = MetaData(item); //this simply begs for cooking refactor
                    if (metaData.EntityPrototype is not null)
                        solidID = metaData.EntityPrototype.ID;
                }

                if (solidID is null)
                    continue;

                if (!solidsDict.TryAdd(solidID, amountToAdd))
                    solidsDict[solidID] += amountToAdd;

                // only use reagents we have access to
                // you have to break the eggs before we can use them!
                if (!_solutionContainer.TryGetDrainableSolution(item, out var _, out var solution))
                    continue;

                foreach (var (reagent, quantity) in solution.Contents)
                    if (!reagentDict.TryAdd(reagent.Prototype, quantity))
                        reagentDict[reagent.Prototype] += quantity;
            }

            // Check recipes
            var getRecipesEv = new GetSecretRecipesEvent();
            RaiseLocalEvent(uid, ref getRecipesEv);

            List<FoodRecipePrototype> recipes = getRecipesEv.Recipes;
            recipes.AddRange(_recipeManager.Recipes);
            var portionedRecipes = recipes.Select(r => CanSatisfyRecipe(component, r, solidsDict, reagentDict)).Where(r => r.Item2 > 0).ToList(); // Starlight-edit

            _audio.PlayPvs(component.StartCookingSound, uid);
            
            // Starlight-start
            component.StartedCookTime = _gameTiming.CurTime;
            var activeComp = AddComp<ActiveCookingDeviceComponent>(uid); //microwave is now cooking
            // Starlight-end
            
            activeComp.CookTimeRemaining = component.CurrentCookTimerTime * component.CookTimeMultiplier;
            activeComp.TotalTime = component.CurrentCookTimerTime; //this doesn't scale so that we can have the "actual" time
            
            // Starlight-start
            foreach (var recipe in portionedRecipes)
                if (!activeComp.PortionedRecipes.ContainsKey(recipe.Item1))
                    activeComp.PortionedRecipes.Add(recipe.Item1, recipe.Item2);
            // Starlight-end
            
            //Scale tiems with cook times
            component.CurrentCookTimeEnd = _gameTiming.CurTime + TimeSpan.FromSeconds(component.CurrentCookTimerTime * component.CookTimeMultiplier);
            if (malfunctioning)
                activeComp.MalfunctionTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.MalfunctionInterval);
            UpdateUserInterfaceState(uid, component);
        }

        private void StopCooking(Entity<CookingDeviceComponent> ent) // Starlight-edit
        {
            RemCompDeferred<ActiveCookingDeviceComponent>(ent); // Starlight-edit
            foreach (var solid in ent.Comp.Storage.ContainedEntities)
                RemCompDeferred<ActivelyCookedComponent>(solid); // Starlight-edit
        }

        public static (FoodRecipePrototype, int) CanSatisfyRecipe(CookingDeviceComponent component, FoodRecipePrototype recipe, Dictionary<string, int> solids, Dictionary<string, FixedPoint2> reagents) // Starlight-edit
        {
            var portions = 0;

            if (component.Safe && component.CurrentCookTimerTime % recipe.CookTime != 0) // Starlight-edit
            {
                //can't be a multiple of this recipe
                return (recipe, 0);
            }

            if (recipe.DeviceType != component.DeviceType)
                return (recipe, 0);

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
            return (recipe, component.Safe ? (int)Math.Min(portions, component.CurrentCookTimerTime / recipe.CookTime) : portions); // Starlight-edit
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ActiveCookingDeviceComponent, CookingDeviceComponent>(); // Starlight-edit
            while (query.MoveNext(out var uid, out var active, out var cookingDevice)) // Starlight-edit
            {

                active.CookTimeRemaining -= frameTime;

                RollMalfunction((uid, active, cookingDevice)); // Starlight-edit

                //check if there's still cook time left
                // Starlight-start
                int actualTime = (int)(_gameTiming.CurTime - cookingDevice.StartedCookTime).TotalSeconds;
                var coords = Transform(uid).Coordinates;
                if (active.CookTimeRemaining > 0 || (!cookingDevice.Safe && actualTime < 60))
                {
                    AddTemperature(cookingDevice, frameTime);
                    continue;
                }
                // Starlight-end

                //this means the microwave has finished cooking.
                AddTemperature(cookingDevice, Math.Max(frameTime + active.CookTimeRemaining, 0)); //Though there's still a little bit more heat to pump out
                
                // Starlight-start
                if (actualTime >= 60)
                {
                    var containedItems = cookingDevice.Storage.ContainedEntities.ToList(); // error-proof copy
                    foreach (var item in containedItems)
                    {
                        string? itemID = null;

                        if (TryComp<StackComponent>(item, out var stackComp))
                            itemID = _prototype.Index<StackPrototype>(stackComp.StackTypeId).Spawn;
                        else
                        {
                            var metaData = MetaData(item);
                            if (metaData.EntityPrototype == null)
                                continue;
                            itemID = metaData.EntityPrototype.ID;
                        }

                        if (stackComp is not null)
                        {
                            if (stackComp.Count == 1)
                                _container.Remove(item, cookingDevice.Storage);
                            _stack.Use(item, 1, stackComp);
                            Spawn(cookingDevice.SpoiledItemId, coords);
                            continue;
                        }
                        else
                        {
                            _container.Remove(item, cookingDevice.Storage);
                            Del(item);
                            Spawn(cookingDevice.SpoiledItemId, coords);
                            continue;
                        }
                    }
                }
                // Starlight-end
                
                foreach (var (recipe, availableAmount) in active.PortionedRecipes) // Starlight-edit
                {
                    int targetTime = (int)recipe.CookTime; // Starlight-edit

                    if (Math.Abs(targetTime - actualTime) <= 1) // Starlight-edit
                    {
                        for (var i = 0; i < availableAmount; i++) // Starlight-edit
                        {
                            if (SubtractContents(cookingDevice, recipe))
                                Spawn(recipe.Result, coords);
                            else
                                continue;
                        }
                    }
                }

                // Starlight-start
                _container.EmptyContainer(cookingDevice.Storage);
                cookingDevice.CurrentCookTimeEnd = TimeSpan.Zero;
                UpdateUserInterfaceState(uid, cookingDevice);
                _audio.PlayPvs(cookingDevice.FoodDoneSound, uid);
                StopCooking((uid, cookingDevice));
                // Starlight-end
            }
        }

        /// <summary>
        /// This event tries to get secret recipes that the microwave might be capable of.
        /// Currently, we only check the microwave itself, but in the future, the user might be able to learn recipes.
        /// </summary>
        private void OnGetSecretRecipes(Entity<FoodRecipeProviderComponent> ent, ref GetSecretRecipesEvent args)
        {
            foreach (ProtoId<FoodRecipePrototype> recipeId in ent.Comp.ProvidedRecipes)
            {
                if (_prototype.Resolve(recipeId, out var recipeProto))
                {
                    args.Recipes.Add(recipeProto);
                }
            }
        }

        #region ui
        
        // Starlight-start
        private void OnStopMessage(Entity<CookingDeviceComponent> ent, ref MicrowaveStopCookMessage args)
        {
            var uid = ent.Owner;
            var cookingDevice = ent.Comp;
            
            if (!TryComp<ActiveCookingDeviceComponent>(ent.Owner, out var active))
                return;
            //this means the microwave has finished cooking.
            AddTemperature(cookingDevice, Math.Max((float)_gameTiming.CurTime.TotalSeconds + active.CookTimeRemaining, 0)); //Though there's still a little bit more heat to pump out
            int actualTime = (int)(_gameTiming.CurTime - cookingDevice.StartedCookTime).TotalSeconds;
            foreach (var (recipe, availableAmount) in active.PortionedRecipes)
            {
                int targetTime = (int)recipe.CookTime;
                var coords = Transform(uid).Coordinates;
                
                if (Math.Abs(targetTime - actualTime) <= 1)
                {
                    for (var i = 0; i < availableAmount; i++)
                    {
                        if (SubtractContents(cookingDevice, recipe))
                            Spawn(recipe.Result, coords);
                        else
                            continue;
                    }
                }
            }

            _container.EmptyContainer(cookingDevice.Storage);
            cookingDevice.CurrentCookTimeEnd = TimeSpan.Zero;
            UpdateUserInterfaceState(uid, cookingDevice);
            _audio.PlayPvs(cookingDevice.FoodDoneSound, uid);
            StopCooking((uid, cookingDevice));
        }
        // Starlight-end
        
        private void OnEjectMessage(Entity<CookingDeviceComponent> ent, ref MicrowaveEjectMessage args) // Starlight-edit
        {
            if (!HasContents(ent.Comp) || HasComp<ActiveCookingDeviceComponent>(ent)) // Starlight-edit
                return;

            _container.EmptyContainer(ent.Comp.Storage);
            _audio.PlayPvs(ent.Comp.ClickSound, ent, AudioParams.Default.WithVolume(-2));
            UpdateUserInterfaceState(ent, ent.Comp);
        }

        private void OnEjectIndex(Entity<CookingDeviceComponent> ent, ref MicrowaveEjectSolidIndexedMessage args) // Starlight-edit
        {
            if (!HasContents(ent.Comp) || HasComp<ActiveCookingDeviceComponent>(ent)) // Starlight-edit
                return;

            _container.Remove(GetEntity(args.EntityID), ent.Comp.Storage);
            UpdateUserInterfaceState(ent, ent.Comp);
        }

        private void OnSelectTime(Entity<CookingDeviceComponent> ent, ref MicrowaveSelectCookTimeMessage args) // Starlight-edit
        {
            if (!HasContents(ent.Comp) || HasComp<ActiveCookingDeviceComponent>(ent) || !(TryComp<ApcPowerReceiverComponent>(ent, out var apc) && apc.Powered)) // Starlight-edit
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
