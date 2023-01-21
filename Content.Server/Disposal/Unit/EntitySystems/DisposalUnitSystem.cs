using System.Linq;
using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Disposal.Unit.Components;
using Content.Server.DoAfter;
using Content.Server.Hands.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.Atmos;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Disposal;
using Content.Shared.Disposal.Components;
using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Map.Components;

namespace Content.Server.Disposal.Unit.EntitySystems
{
    public sealed class DisposalUnitSystem : SharedDisposalUnitSystem
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly PowerReceiverSystem _power = default!;

        public override void Initialize()
        {
            base.Initialize();

            // Shouldn't need re-anchoring.
            SubscribeLocalEvent<DisposalUnitComponent, AnchorStateChangedEvent>(OnAnchorChanged);
            // TODO: Predict me when hands predicted
            SubscribeLocalEvent<DisposalUnitComponent, ContainerRelayMovementEntityEvent>(HandleMovement);
            SubscribeLocalEvent<DisposalUnitComponent, PowerChangedEvent>(HandlePowerChange);

            // Component lifetime
            SubscribeLocalEvent<DisposalUnitComponent, ComponentInit>(HandleDisposalInit);
            SubscribeLocalEvent<DisposalUnitComponent, ComponentRemove>(HandleDisposalRemove);

            SubscribeLocalEvent<DisposalUnitComponent, ThrowHitByEvent>(HandleThrowCollide);

            // Interactions
            SubscribeLocalEvent<DisposalUnitComponent, ActivateInWorldEvent>(HandleActivate);
            SubscribeLocalEvent<DisposalUnitComponent, AfterInteractUsingEvent>(HandleAfterInteractUsing);
            SubscribeLocalEvent<DisposalUnitComponent, DragDropEvent>(HandleDragDropOn);
            SubscribeLocalEvent<DisposalUnitComponent, DestructionEventArgs>(HandleDestruction);

            // Verbs
            SubscribeLocalEvent<DisposalUnitComponent, GetVerbsEvent<InteractionVerb>>(AddInsertVerb);
            SubscribeLocalEvent<DisposalUnitComponent, GetVerbsEvent<AlternativeVerb>>(AddDisposalAltVerbs);
            SubscribeLocalEvent<DisposalUnitComponent, GetVerbsEvent<Verb>>(AddClimbInsideVerb);

            // Units
            SubscribeLocalEvent<DoInsertDisposalUnitEvent>(DoInsertDisposalUnit);

            //UI
            SubscribeLocalEvent<DisposalUnitComponent, SharedDisposalUnitComponent.UiButtonPressedMessage>(OnUiButtonPressed);
        }

        private void AddDisposalAltVerbs(EntityUid disposalUnit, DisposalUnitComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            // Behavior for if the disposals bin has items in it
            if (component.Container.ContainedEntities.Count > 0)
            {
                // Verbs to flush the unit
                AlternativeVerb flushVerb = new();
                flushVerb.Act = () => Engage(disposalUnit, component);
                flushVerb.Text = Loc.GetString("disposal-flush-verb-get-data-text");
                flushVerb.IconTexture = "/Textures/Interface/VerbIcons/delete_transparent.svg.192dpi.png";
                flushVerb.Priority = 1;
                args.Verbs.Add(flushVerb);

                // Verb to eject the contents
                AlternativeVerb ejectVerb = new()
                {
                    Act = () => TryEjectContents(disposalUnit, component),
                    Category = VerbCategory.Eject,
                    Text = Loc.GetString("disposal-eject-verb-get-data-text")
                };
                args.Verbs.Add(ejectVerb);
            }
        }

        private void AddClimbInsideVerb(EntityUid uid, DisposalUnitComponent component, GetVerbsEvent<Verb> args)
        {
            // This is not an interaction, activation, or alternative verb type because unfortunately most users are
            // unwilling to accept that this is where they belong and don't want to accidentally climb inside.
            if (!component.MobsCanEnter ||
                !args.CanAccess ||
                !args.CanInteract ||
                component.Container.ContainedEntities.Contains(args.User) ||
                !_actionBlockerSystem.CanMove(args.User))
                return;

            // Add verb to climb inside of the unit,
            Verb verb = new()
            {
                Act = () => TryInsert(uid, args.User, args.User),
                DoContactInteraction = true,
                Text = Loc.GetString("disposal-self-insert-verb-get-data-text")
            };
            // TODO VERB ICON
            // TODO VERB CATEGORY
            // create a verb category for "enter"?
            // See also, medical scanner. Also maybe add verbs for entering lockers/body bags?
            args.Verbs.Add(verb);
        }

        private void AddInsertVerb(EntityUid uid, DisposalUnitComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || args.Hands == null || args.Using == null)
                return;

            if (!_actionBlockerSystem.CanDrop(args.User))
                return;

            if (!CanInsert(component, args.Using.Value))
                return;

            InteractionVerb insertVerb = new()
            {
                Text = Name(args.Using.Value),
                Category = VerbCategory.Insert,
                Act = () =>
                {
                    _handsSystem.TryDropIntoContainer(args.User, args.Using.Value, component.Container, checkActionBlocker: false, args.Hands);
                    _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User):player} inserted {ToPrettyString(args.Using.Value)} into {ToPrettyString(uid)}");
                    AfterInsert(uid, component, args.Using.Value);
                }
            };

            args.Verbs.Add(insertVerb);
        }

        private void DoInsertDisposalUnit(DoInsertDisposalUnitEvent ev)
        {
            var toInsert = ev.ToInsert;

            if (!EntityManager.TryGetComponent(ev.Unit, out DisposalUnitComponent? disposalUnit))
            {
                return;
            }

            if (!disposalUnit.Container.Insert(toInsert))
                return;
            if (ev.User != null)
                _adminLogger.Add(LogType.Action, LogImpact.Medium,
                    $"{ToPrettyString(ev.User.Value):player} inserted {ToPrettyString(toInsert)} into {ToPrettyString(ev.Unit)}");
            AfterInsert(ev.Unit, disposalUnit, toInsert);
        }

        public void DoInsertDisposalUnit(EntityUid disposalUnit, EntityUid toInsert, EntityUid user, DisposalUnitComponent? disposal = null)
        {
            if (!Resolve(disposalUnit, ref disposal))
                return;

            if (!disposal.Container.Insert(toInsert))
                return;

            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):player} inserted {ToPrettyString(toInsert)} into {ToPrettyString(disposalUnit)}");
            AfterInsert(disposalUnit, disposal, toInsert);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var (_, comp) in EntityQuery<ActiveDisposalUnitComponent, DisposalUnitComponent>())
            {
                var disposalUnit = comp.Owner;
                if (!Update(disposalUnit, comp, frameTime))
                    continue;

                RemComp<ActiveDisposalUnitComponent>(disposalUnit);
            }
        }

        #region UI Handlers
        private void OnUiButtonPressed(EntityUid disposalUnit, DisposalUnitComponent component, SharedDisposalUnitComponent.UiButtonPressedMessage args)
        {
            if (args.Session.AttachedEntity is not {Valid: true} player)
            {
                return;
            }

            switch (args.Button)
            {
                case SharedDisposalUnitComponent.UiButton.Eject:
                    TryEjectContents(disposalUnit, component);
                    _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):player} hit eject button on {ToPrettyString(disposalUnit)}");
                    break;
                case SharedDisposalUnitComponent.UiButton.Engage:
                    ToggleEngage(disposalUnit, component);
                    _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):player} hit flush button on {ToPrettyString(disposalUnit)}, it's now {(component.Engaged ? "on" : "off")}");
                    break;
                case SharedDisposalUnitComponent.UiButton.Power:
                    _power.TogglePower(disposalUnit, user: args.Session.AttachedEntity);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void ToggleEngage(EntityUid disposalUnit, DisposalUnitComponent component)
        {
            component.Engaged ^= true;

            if (component.Engaged)
            {
                Engage(disposalUnit, component);
            }
            else
            {
                Disengage(disposalUnit, component);
            }
        }
        #endregion

        #region Eventbus Handlers
        private void HandleActivate(EntityUid uid, DisposalUnitComponent component, ActivateInWorldEvent args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            {
                return;
            }

            args.Handled = true;
            _ui.TryOpen(uid, SharedDisposalUnitComponent.DisposalUnitUiKey.Key, actor.PlayerSession);
        }

        private void HandleAfterInteractUsing(EntityUid uid, DisposalUnitComponent component, AfterInteractUsingEvent args)
        {
            if (args.Handled || !args.CanReach)
                return;

            if (!EntityManager.HasComponent<HandsComponent>(args.User))
            {
                return;
            }

            if (!CanInsert(component, args.Used) || !_handsSystem.TryDropIntoContainer(args.User, args.Used, component.Container))
            {
                return;
            }

            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User):player} inserted {ToPrettyString(args.Used)} into {ToPrettyString(uid)}");
            AfterInsert(uid, component, args.Used);
            args.Handled = true;
        }

        /// <summary>
        /// Thrown items have a chance of bouncing off the unit and not going in.
        /// </summary>
        private void HandleThrowCollide(EntityUid uid, DisposalUnitComponent component, ThrowHitByEvent args)
        {
            if (!CanInsert(component, args.Thrown) ||
                _robustRandom.NextDouble() > 0.75 ||
                !component.Container.Insert(args.Thrown))
            {
                _popupSystem.PopupEntity(Loc.GetString("disposal-unit-thrown-missed"), uid);
                return;
            }

            if (args.User != null)
                _adminLogger.Add(LogType.Landed, LogImpact.Low, $"{ToPrettyString(args.Thrown)} thrown by {ToPrettyString(args.User.Value):player} landed in {ToPrettyString(uid)}");
            AfterInsert(uid, component, args.Thrown);
        }

        private void HandleDisposalInit(EntityUid uid, DisposalUnitComponent component, ComponentInit args)
        {
            component.Container = _containerSystem.EnsureContainer<Container>(uid, SharedDisposalUnitComponent.ContainerId);

            UpdateInterface(uid, component, component.Powered);

            if (!EntityManager.HasComponent<AnchorableComponent>(uid))
            {
                Logger.WarningS("VitalComponentMissing", $"Disposal unit {uid} is missing an {nameof(AnchorableComponent)}");
            }
        }

        private void HandleDisposalRemove(EntityUid uid, DisposalUnitComponent component, ComponentRemove args)
        {
            foreach (var entity in component.Container.ContainedEntities.ToArray())
            {
                component.Container.Remove(entity, force: true);
            }

            _ui.TryCloseAll(uid, SharedDisposalUnitComponent.DisposalUnitUiKey.Key);
            component.AutomaticEngageToken?.Cancel();
            component.AutomaticEngageToken = null;

            component.Container = null!;
            RemComp<ActiveDisposalUnitComponent>(uid);
        }

        private void HandlePowerChange(EntityUid uid, DisposalUnitComponent component, ref PowerChangedEvent args)
        {
            if (!component.Running)
                return;

            component.Powered = args.Powered;

            // TODO: Need to check the other stuff.
            if (!args.Powered)
            {
                component.AutomaticEngageToken?.Cancel();
                component.AutomaticEngageToken = null;
            }

            HandleStateChange(uid, component, args.Powered && component.State == SharedDisposalUnitComponent.PressureState.Pressurizing);
            UpdateVisualState(uid, component);
            UpdateInterface(uid, component, args.Powered);

            if (component.Engaged && !TryFlush(uid, component))
            {
                TryQueueEngage(uid, component);
            }
        }

        /// <summary>
        /// Add or remove this disposal from the active ones for updating.
        /// </summary>
        public void HandleStateChange(EntityUid disposalUnit, DisposalUnitComponent component, bool active)
        {
            if (active)
            {
                EnsureComp<ActiveDisposalUnitComponent>(disposalUnit);
            }
            else
            {
                RemComp<ActiveDisposalUnitComponent>(disposalUnit);
            }
        }

        private void HandleMovement(EntityUid uid, DisposalUnitComponent component, ref ContainerRelayMovementEntityEvent args)
        {
            var currentTime = GameTiming.CurTime;

            if (!EntityManager.TryGetComponent(args.Entity, out HandsComponent? hands) ||
                hands.Count == 0 ||
                currentTime < component.LastExitAttempt + ExitAttemptDelay)
            {
                return;
            }

            component.LastExitAttempt = currentTime;
            Remove(uid, component, args.Entity);
        }

        private void OnAnchorChanged(EntityUid uid, DisposalUnitComponent component, ref AnchorStateChangedEvent args)
        {
            if (Terminating(uid))
                return;

            UpdateVisualState(uid, component);
            if (!args.Anchored)
                TryEjectContents(uid, component);
        }

        private void HandleDestruction(EntityUid uid, DisposalUnitComponent component, DestructionEventArgs args)
        {
            TryEjectContents(uid, component);
        }

        private void HandleDragDropOn(EntityUid uid, DisposalUnitComponent component, DragDropEvent args)
        {
            args.Handled = TryInsert(uid, args.Dragged, args.User);
        }
        #endregion

        /// <summary>
        /// Work out if we can stop updating this disposals component i.e. full pressure and nothing colliding.
        /// </summary>
        private bool Update(EntityUid disposalUnit, DisposalUnitComponent component, float frameTime)
        {
            var oldPressure = component.Pressure;

            component.Pressure = MathF.Min(1.0f, component.Pressure + PressurePerSecond * frameTime);
            component.State = component.Pressure >= 1 ? SharedDisposalUnitComponent.PressureState.Ready : SharedDisposalUnitComponent.PressureState.Pressurizing;

            var state = component.State;

            if (oldPressure < 1 && state == SharedDisposalUnitComponent.PressureState.Ready)
            {
                UpdateVisualState(disposalUnit, component);
                UpdateInterface(disposalUnit, component, component.Powered);

                if (component.Engaged)
                {
                    TryFlush(disposalUnit, component);
                    state = component.State;
                }
            }

            if (component.State == SharedDisposalUnitComponent.PressureState.Pressurizing)
            {
                var oldTimeElapsed = oldPressure / PressurePerSecond;
                if (oldTimeElapsed < component.FlushTime && (oldTimeElapsed + frameTime) >= component.FlushTime)
                {
                    // We've crossed over the amount of time it takes to flush. This will switch the
                    // visuals over to a 'Charging' state.
                    UpdateVisualState(disposalUnit, component);
                }
            }

            Box2? disposalsBounds = null;
            var count = component.RecentlyEjected.Count;

            if (count > 0)
            {
                if (!EntityManager.TryGetComponent(disposalUnit, out PhysicsComponent? disposalsBody))
                {
                    component.RecentlyEjected.Clear();
                }
                else
                {
                    disposalsBounds = _lookup.GetWorldAABB(disposalUnit);
                }
            }

            for (var i = component.RecentlyEjected.Count - 1; i >= 0; i--)
            {
                var uid = component.RecentlyEjected[i];
                if (EntityManager.EntityExists(uid) &&
                    EntityManager.TryGetComponent(uid, out PhysicsComponent? body))
                {
                    // TODO: We need to use a specific collision method (which sloth hasn't coded yet) for actual bounds overlaps.
                    // Check for itemcomp as we won't just block the disposal unit "sleeping" for something it can't collide with anyway.
                    if (!EntityManager.HasComponent<ItemComponent>(uid) && _lookup.GetWorldAABB(uid).Intersects(disposalsBounds!.Value)) continue;
                    component.RecentlyEjected.RemoveAt(i);
                }
            }

            if (count != component.RecentlyEjected.Count)
                Dirty(component);

            return state == SharedDisposalUnitComponent.PressureState.Ready && component.RecentlyEjected.Count == 0;
        }

        public bool TryInsert(EntityUid unitId, EntityUid toInsertId, EntityUid? userId, DisposalUnitComponent? unit = null)
        {
            if (!Resolve(unitId, ref unit))
                return false;

            if (userId.HasValue && !HasComp<SharedHandsComponent>(userId) && toInsertId != userId) // Mobs like mouse can Jump inside even with no hands
            {
                _popupSystem.PopupEntity(Loc.GetString("disposal-unit-no-hands"), userId.Value, userId.Value, PopupType.SmallCaution);
                return false;
            }

            if (!CanInsert(unit, toInsertId))
                return false;

            var delay = userId == toInsertId ? unit.EntryDelay : unit.DraggedEntryDelay;
            var ev = new DoInsertDisposalUnitEvent(userId, toInsertId, unitId);

            if (delay <= 0 || userId == null)
            {
                DoInsertDisposalUnit(ev);
                return true;
            }

            // Can't check if our target AND disposals moves currently so we'll just check target.
            // if you really want to check if disposals moves then add a predicate.
            var doAfterArgs = new DoAfterEventArgs(userId.Value, delay, default, toInsertId)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = false,
                BroadcastFinishedEvent = ev
            };

            _doAfterSystem.DoAfter(doAfterArgs);
            return true;
        }


        public bool TryFlush(EntityUid disposalUnit, DisposalUnitComponent component)
        {
            if (component.Deleted || !CanFlush(disposalUnit, component))
            {
                return false;
            }

            //Allows the MailingUnitSystem to add tags or prevent flushing
            var beforeFlushArgs = new BeforeDisposalFlushEvent();
            RaiseLocalEvent(disposalUnit, beforeFlushArgs);

            if (beforeFlushArgs.Cancelled)
            {
                Disengage(disposalUnit, component);
                return false;
            }

            var xform = Transform(disposalUnit);
            if (!TryComp(xform.GridUid, out MapGridComponent? grid))
                return false;

            var coords = xform.Coordinates;
            var entry = grid.GetLocal(coords)
                .FirstOrDefault(entity => EntityManager.HasComponent<DisposalEntryComponent>(entity));

            if (entry == default)
            {
                return false;
            }

            var air = component.Air;
            var entryComponent = EntityManager.GetComponent<DisposalEntryComponent>(entry);
            var indices = _transformSystem.GetGridOrMapTilePosition(disposalUnit, xform);

            if (_atmosSystem.GetTileMixture(xform.GridUid, xform.MapUid, indices, true) is {Temperature: > 0} environment)
            {
                var transferMoles = 0.1f * (0.25f * Atmospherics.OneAtmosphere * 1.01f - air.Pressure) * air.Volume / (environment.Temperature * Atmospherics.R);

                component.Air = environment.Remove(transferMoles);
            }

            entryComponent.TryInsert(component, beforeFlushArgs.Tags);

            component.AutomaticEngageToken?.Cancel();
            component.AutomaticEngageToken = null;

            component.Pressure = 0;
            component.State = SharedDisposalUnitComponent.PressureState.Pressurizing;

            component.Engaged = false;

            HandleStateChange(disposalUnit, component, true);
            UpdateVisualState(disposalUnit, component, true);
            UpdateInterface(disposalUnit, component, component.Powered);

            return true;
        }

        public void UpdateInterface(EntityUid disposalUnit, DisposalUnitComponent component, bool powered)
        {
            var stateString = Loc.GetString($"disposal-unit-state-{component.State}");
            var state = new SharedDisposalUnitComponent.DisposalUnitBoundUserInterfaceState(Name(disposalUnit), stateString, EstimatedFullPressure(component), powered, component.Engaged);
            _ui.TrySetUiState(disposalUnit, SharedDisposalUnitComponent.DisposalUnitUiKey.Key, state);

            var stateUpdatedEvent = new DisposalUnitUIStateUpdatedEvent(state);
            RaiseLocalEvent(disposalUnit, stateUpdatedEvent);
        }

        private TimeSpan EstimatedFullPressure(DisposalUnitComponent component)
        {
            if (component.State == SharedDisposalUnitComponent.PressureState.Ready) return TimeSpan.Zero;

            var currentTime = GameTiming.CurTime;
            var pressure = component.Pressure;

            return TimeSpan.FromSeconds(currentTime.TotalSeconds + (1.0f - pressure) / PressurePerSecond);
        }

        public void UpdateVisualState(EntityUid unit, DisposalUnitComponent component)
        {
            UpdateVisualState(unit, component, false);
        }

        public void UpdateVisualState(EntityUid unit, DisposalUnitComponent component, bool flush)
        {
            if (!EntityManager.TryGetComponent(unit, out AppearanceComponent? appearance))
            {
                return;
            }

            if (!EntityManager.GetComponent<TransformComponent>(unit).Anchored)
            {
                _appearance.SetData(unit, SharedDisposalUnitComponent.Visuals.VisualState, SharedDisposalUnitComponent.VisualState.UnAnchored, appearance);
                _appearance.SetData(unit, SharedDisposalUnitComponent.Visuals.Handle, SharedDisposalUnitComponent.HandleState.Normal, appearance);
                _appearance.SetData(unit, SharedDisposalUnitComponent.Visuals.Light, SharedDisposalUnitComponent.LightState.Off, appearance);
                return;
            }

            _appearance.SetData(unit, SharedDisposalUnitComponent.Visuals.VisualState, component.Pressure < 1 ? SharedDisposalUnitComponent.VisualState.Charging : SharedDisposalUnitComponent.VisualState.Anchored, appearance);

            _appearance.SetData(unit, SharedDisposalUnitComponent.Visuals.Handle, component.Engaged
                ? SharedDisposalUnitComponent.HandleState.Engaged
                : SharedDisposalUnitComponent.HandleState.Normal, appearance);

            if (!component.Powered)
            {
                _appearance.SetData(unit, SharedDisposalUnitComponent.Visuals.Light, SharedDisposalUnitComponent.LightState.Off, appearance);
                return;
            }

            if (flush)
            {
                _appearance.SetData(unit, SharedDisposalUnitComponent.Visuals.VisualState, SharedDisposalUnitComponent.VisualState.Flushing, appearance);
                _appearance.SetData(unit, SharedDisposalUnitComponent.Visuals.Light, SharedDisposalUnitComponent.LightState.Off, appearance);
                return;
            }

            if (component.Container.ContainedEntities.Count > 0)
            {
                _appearance.SetData(unit, SharedDisposalUnitComponent.Visuals.Light, SharedDisposalUnitComponent.LightState.Full, appearance);
                return;
            }

            _appearance.SetData(unit, SharedDisposalUnitComponent.Visuals.Light, component.Pressure < 1
                ? SharedDisposalUnitComponent.LightState.Charging
                : SharedDisposalUnitComponent.LightState.Ready, appearance);
        }

        public void Remove(EntityUid disposalUnit, DisposalUnitComponent component, EntityUid toRemove)
        {
            component.Container.Remove(toRemove);

            if (component.Container.ContainedEntities.Count == 0)
            {
                component.AutomaticEngageToken?.Cancel();
                component.AutomaticEngageToken = null;
            }

            if (!component.RecentlyEjected.Contains(toRemove))
                component.RecentlyEjected.Add(toRemove);

            Dirty(component);
            HandleStateChange(disposalUnit, component, active: true);
            UpdateVisualState(disposalUnit, component);
        }

        public bool CanFlush(EntityUid unit, DisposalUnitComponent component)
        {
            return component.State == SharedDisposalUnitComponent.PressureState.Ready
                && component.Powered
                && EntityManager.GetComponent<TransformComponent>(unit).Anchored;
        }

        public void Engage(EntityUid disposalUnit, DisposalUnitComponent component)
        {
            component.Engaged = true;
            UpdateVisualState(disposalUnit, component);
            UpdateInterface(disposalUnit, component, component.Powered);

            if (CanFlush(disposalUnit, component))
            {
                disposalUnit.SpawnTimer(component.FlushDelay, () => TryFlush(disposalUnit, component));
            }
        }

        public void Disengage(EntityUid disposalUnit, DisposalUnitComponent component)
        {
            component.Engaged = false;
            UpdateVisualState(disposalUnit, component);
            UpdateInterface(disposalUnit, component, component.Powered);
        }

        /// <summary>
        /// Remove all entities currently in the disposal unit.
        /// </summary>
        public void TryEjectContents(EntityUid disposalUnit, DisposalUnitComponent component)
        {
            foreach (var entity in component.Container.ContainedEntities.ToArray())
            {
                Remove(disposalUnit, component, entity);
            }
        }

        public override bool CanInsert(SharedDisposalUnitComponent component, EntityUid entity)
        {
            if (!base.CanInsert(component, entity) || component is not DisposalUnitComponent serverComp)
                return false;

            return serverComp.Container.CanInsert(entity);
        }

        /// <summary>
        /// If something is inserted (or the likes) then we'll queue up a flush in the future.
        /// </summary>
        public void TryQueueEngage(EntityUid disposalUnit, DisposalUnitComponent component)
        {
            if (component.Deleted || !component.AutomaticEngage || !component.Powered && component.Container.ContainedEntities.Count == 0)
            {
                return;
            }

            component.AutomaticEngageToken = new CancellationTokenSource();

            disposalUnit.SpawnTimer(component.AutomaticEngageTime, () =>
            {
                if (!TryFlush(disposalUnit, component))
                {
                    TryQueueEngage(disposalUnit, component);
                }
            }, component.AutomaticEngageToken.Token);
        }

        public void AfterInsert(EntityUid disposalUnit, DisposalUnitComponent component, EntityUid inserted)
        {
            TryQueueEngage(disposalUnit, component);

            if (EntityManager.TryGetComponent(inserted, out ActorComponent? actor))
            {
                _ui.TryClose(disposalUnit, SharedDisposalUnitComponent.DisposalUnitUiKey.Key, actor.PlayerSession);
            }

            UpdateVisualState(disposalUnit, component);
        }
    }

    /// <summary>
    /// Sent before the disposal unit flushes it's contents.
    /// Allows adding tags for sorting and preventing the disposal unit from flushing.
    /// </summary>
    public sealed class DisposalUnitUIStateUpdatedEvent : EntityEventArgs
    {
        public SharedDisposalUnitComponent.DisposalUnitBoundUserInterfaceState State;

        public DisposalUnitUIStateUpdatedEvent(SharedDisposalUnitComponent.DisposalUnitBoundUserInterfaceState state)
        {
            State = state;
        }
    }

    /// <summary>
    /// Sent before the disposal unit flushes it's contents.
    /// Allows adding tags for sorting and preventing the disposal unit from flushing.
    /// </summary>
    public sealed class BeforeDisposalFlushEvent : CancellableEntityEventArgs
    {
        public readonly List<string> Tags = new();
    }
}
