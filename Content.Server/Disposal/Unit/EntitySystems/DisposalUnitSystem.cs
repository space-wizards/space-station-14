using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction.Components;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Disposal.Unit.Components;
using Content.Server.DoAfter;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Power.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Atmos;
using Content.Shared.Disposal;
using Content.Shared.Disposal.Components;
using Content.Shared.Interaction;
using Content.Shared.Movement;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server.Disposal.Unit.EntitySystems
{
    public sealed class DisposalUnitSystem : SharedDisposalUnitSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;

        private readonly List<DisposalUnitComponent> _activeDisposals = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DisposalUnitComponent, AnchoredEvent>(OnAnchored);
            SubscribeLocalEvent<DisposalUnitComponent, UnanchoredEvent>(OnUnanchored);
            // TODO: Predict me when hands predicted
            SubscribeLocalEvent<DisposalUnitComponent, RelayMovementEntityEvent>(HandleMovement);
            SubscribeLocalEvent<DisposalUnitComponent, PowerChangedEvent>(HandlePowerChange);

            // Component lifetime
            SubscribeLocalEvent<DisposalUnitComponent, ComponentInit>(HandleDisposalInit);
            SubscribeLocalEvent<DisposalUnitComponent, ComponentShutdown>(HandleDisposalShutdown);

            SubscribeLocalEvent<DisposalUnitComponent, ThrowHitByEvent>(HandleThrowCollide);

            // Interactions
            SubscribeLocalEvent<DisposalUnitComponent, ActivateInWorldEvent>(HandleActivate);
            SubscribeLocalEvent<DisposalUnitComponent, InteractHandEvent>(HandleInteractHand);
            SubscribeLocalEvent<DisposalUnitComponent, InteractUsingEvent>(HandleInteractUsing);

            // Verbs
            SubscribeLocalEvent<DisposalUnitComponent, GetAlternativeVerbsEvent>(AddFlushEjectVerbs);
            SubscribeLocalEvent<DisposalUnitComponent, GetOtherVerbsEvent>(AddClimbInsideVerb);

            // Units
            SubscribeLocalEvent<DoInsertDisposalUnitEvent>(DoInsertDisposalUnit);
        }

        private void AddFlushEjectVerbs(EntityUid uid, DisposalUnitComponent component, GetAlternativeVerbsEvent args)
        {
            if (!args.CanAccess || !args.CanInteract || component.ContainedEntities.Count == 0)
                return;

            // Verbs to flush the unit
            Verb flushVerb = new();
            flushVerb.Act = () => Engage(component);
            flushVerb.Text = Loc.GetString("disposal-flush-verb-get-data-text");
            flushVerb.IconTexture = "/Textures/Interface/VerbIcons/delete_transparent.svg.192dpi.png";
            flushVerb.Priority = 1;
            args.Verbs.Add(flushVerb);

            // Verb to eject the contents
            Verb ejectVerb = new();
            ejectVerb.Act = () => TryEjectContents(component);
            ejectVerb.Category = VerbCategory.Eject;
            ejectVerb.Text = Loc.GetString("disposal-eject-verb-contents");
            args.Verbs.Add(ejectVerb);
        }

        private void AddClimbInsideVerb(EntityUid uid, DisposalUnitComponent component, GetOtherVerbsEvent args)
        {
            // This is not an interaction, activation, or alternative verb type because unfortunately most users are
            // unwilling to accept that this is where they belong and don't want to accidentally climb inside.
            if (!args.CanAccess ||
                !args.CanInteract ||
                component.ContainedEntities.Contains(args.User) ||
                !_actionBlockerSystem.CanMove(args.User))
                return;

            // Add verb to climb inside of the unit,
            Verb verb = new()
            {
                Act = () => TryInsert(component.Owner, args.User, args.User),
                Text = Loc.GetString("disposal-self-insert-verb-get-data-text")
            };
            // TODO VERN ICON
            // TODO VERB CATEGORY
            // create a verb category for "enter"?
            // See also, medical scanner. Also maybe add verbs for entering lockers/body bags?
            args.Verbs.Add(verb);
        }

        private void DoInsertDisposalUnit(DoInsertDisposalUnitEvent ev)
        {
            var toInsert = ev.ToInsert;

            if (!EntityManager.TryGetComponent(ev.Unit, out DisposalUnitComponent? unit))
            {
                return;
            }

            if (!unit.Container.Insert(toInsert))
            {
                return;
            }

            AfterInsert(unit, toInsert);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            for (var i = _activeDisposals.Count - 1; i >= 0; i--)
            {
                var comp = _activeDisposals[i];
                if (!Update(comp, frameTime)) continue;
                _activeDisposals.RemoveAt(i);
            }
        }

        #region UI Handlers
        public void ToggleEngage(DisposalUnitComponent component)
        {
            component.Engaged ^= true;

            if (component.Engaged)
            {
                Engage(component);
            }
            else
            {
                Disengage(component);
            }
        }

        public void TogglePower(DisposalUnitComponent component)
        {
            if (!EntityManager.TryGetComponent(component.Owner, out ApcPowerReceiverComponent? receiver))
            {
                return;
            }

            receiver.PowerDisabled = !receiver.PowerDisabled;
            UpdateInterface(component, receiver.Powered);
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

            if (IsValidInteraction(args))
            {
                component.UserInterface?.Open(actor.PlayerSession);
            }
        }

        private void HandleInteractHand(EntityUid uid, DisposalUnitComponent component, InteractHandEvent args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor)) return;

            // Duplicated code here, not sure how else to get actor inside to make UserInterface happy.

            if (!IsValidInteraction(args)) return;
            component.UserInterface?.Open(actor.PlayerSession);
            args.Handled = true;
        }

        private void HandleInteractUsing(EntityUid uid, DisposalUnitComponent component, InteractUsingEvent args)
        {
            if (!EntityManager.TryGetComponent(args.User, out HandsComponent? hands))
            {
                return;
            }

            if (!CanInsert(component, args.Used) || !hands.Drop(args.Used, component.Container))
            {
                return;
            }

            AfterInsert(component, args.Used);
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
                return;
            }

            AfterInsert(component, args.Thrown);
        }

        private void HandleDisposalInit(EntityUid uid, DisposalUnitComponent component, ComponentInit args)
        {
            component.Container = component.Owner.EnsureContainer<Container>(component.Name);

            if (component.UserInterface != null)
            {
                component.UserInterface.OnReceiveMessage += component.OnUiReceiveMessage;
            }

            UpdateInterface(component, component.Powered);

            if (!EntityManager.HasComponent<AnchorableComponent>(component.Owner))
            {
                Logger.WarningS("VitalComponentMissing", $"Disposal unit {uid} is missing an {nameof(AnchorableComponent)}");
            }
        }

        private void HandleDisposalShutdown(EntityUid uid, DisposalUnitComponent component, ComponentShutdown args)
        {
            foreach (var entity in component.Container.ContainedEntities.ToArray())
            {
                component.Container.ForceRemove(entity);
            }

            component.UserInterface?.CloseAll();

            component.AutomaticEngageToken?.Cancel();
            component.AutomaticEngageToken = null;

            component.Container = null!;
            _activeDisposals.Remove(component);
        }

        private void HandlePowerChange(EntityUid uid, DisposalUnitComponent component, PowerChangedEvent args)
        {
            if (!component.Running)
                return;

            // TODO: Need to check the other stuff.
            if (!args.Powered)
            {
                component.AutomaticEngageToken?.Cancel();
                component.AutomaticEngageToken = null;
            }

            HandleStateChange(component, args.Powered && component.State == SharedDisposalUnitComponent.PressureState.Pressurizing);
            UpdateVisualState(component);
            UpdateInterface(component, args.Powered);

            if (component.Engaged && !TryFlush(component))
            {
                TryQueueEngage(component);
            }
        }

        /// <summary>
        /// Add or remove this disposal from the active ones for updating.
        /// </summary>
        public void HandleStateChange(DisposalUnitComponent component, bool active)
        {
            if (active)
            {
                if (!_activeDisposals.Contains(component))
                    _activeDisposals.Add(component);
            }
            else
            {
                _activeDisposals.Remove(component);
            }
        }

        private void HandleMovement(EntityUid uid, DisposalUnitComponent component, RelayMovementEntityEvent args)
        {
            var currentTime = GameTiming.CurTime;

            if (!EntityManager.TryGetComponent(args.Entity, out HandsComponent? hands) ||
                hands.Count == 0 ||
                currentTime < component.LastExitAttempt + ExitAttemptDelay)
            {
                return;
            }

            component.LastExitAttempt = currentTime;
            Remove(component, args.Entity);
        }

        private void OnAnchored(EntityUid uid, DisposalUnitComponent component, AnchoredEvent args)
        {
            UpdateVisualState(component);
        }

        private void OnUnanchored(EntityUid uid, DisposalUnitComponent component, UnanchoredEvent args)
        {
            UpdateVisualState(component);
            TryEjectContents(component);
        }
        #endregion

        /// <summary>
        /// Work out if we can stop updating this disposals component i.e. full pressure and nothing colliding.
        /// </summary>
        private bool Update(DisposalUnitComponent component, float frameTime)
        {
            var oldPressure = component.Pressure;

            component.Pressure = MathF.Min(1.0f, component.Pressure + PressurePerSecond * frameTime);

            var state = component.State;

            if (oldPressure < 1 && state == SharedDisposalUnitComponent.PressureState.Ready)
            {
                UpdateVisualState(component);

                if (component.Engaged)
                {
                    TryFlush(component);
                }
            }

            Box2? disposalsBounds = null;
            var count = component.RecentlyEjected.Count;

            if (count > 0)
            {
                if (!EntityManager.TryGetComponent(component.Owner, out PhysicsComponent? disposalsBody))
                {
                    component.RecentlyEjected.Clear();
                }
                else
                {
                    disposalsBounds = disposalsBody.GetWorldAABB();
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
                    if (!EntityManager.HasComponent<ItemComponent>(uid) && body.GetWorldAABB().Intersects(disposalsBounds!.Value)) continue;
                    component.RecentlyEjected.RemoveAt(i);
                }
            }

            if (count != component.RecentlyEjected.Count)
                component.Dirty();

            return state == SharedDisposalUnitComponent.PressureState.Ready && component.RecentlyEjected.Count == 0;
        }

        private bool IsValidInteraction(ITargetedInteractEventArgs eventArgs)
        {
            if (!Get<ActionBlockerSystem>().CanInteract(eventArgs.User))
            {
                eventArgs.Target.PopupMessage(eventArgs.User, Loc.GetString("ui-disposal-unit-is-valid-interaction-cannot=interact"));
                return false;
            }

            if (eventArgs.User.IsInContainer())
            {
                eventArgs.Target.PopupMessage(eventArgs.User, Loc.GetString("ui-disposal-unit-is-valid-interaction-cannot-reach"));
                return false;
            }
            // This popup message doesn't appear on clicks, even when code was seperate. Unsure why.

            if (!EntityManager.HasComponent<HandsComponent>(eventArgs.User))
            {
                eventArgs.Target.PopupMessage(eventArgs.User, Loc.GetString("ui-disposal-unit-is-valid-interaction-no-hands"));
                return false;
            }

            return true;
        }

        public void TryInsert(EntityUid unitId, EntityUid toInsertId, EntityUid userId, DisposalUnitComponent? unit = null)
        {
            if (!Resolve(unitId, ref unit))
                return;

            if (!CanInsert(unit, toInsertId))
                return;

            var delay = userId == toInsertId ? unit.EntryDelay : unit.DraggedEntryDelay;
            var ev = new DoInsertDisposalUnitEvent(userId, toInsertId, unitId);

            if (delay <= 0)
            {
                DoInsertDisposalUnit(ev);
                return;
            }

            // Can't check if our target AND disposals moves currently so we'll just check target.
            // if you really want to check if disposals moves then add a predicate.
            var doAfterArgs = new DoAfterEventArgs(userId, delay, default, toInsertId)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = false,
                BroadcastFinishedEvent = ev
            };

            _doAfterSystem.DoAfter(doAfterArgs);
        }


        public bool TryFlush(DisposalUnitComponent component)
        {
            if (component.Deleted || !CanFlush(component))
            {
                return false;
            }

            var grid = _mapManager.GetGrid(EntityManager.GetComponent<TransformComponent>(component.Owner).GridID);
            var coords = EntityManager.GetComponent<TransformComponent>(component.Owner).Coordinates;
            var entry = grid.GetLocal(coords)
                .FirstOrDefault(entity => EntityManager.HasComponent<DisposalEntryComponent>(entity));

            if (entry == default)
            {
                return false;
            }

            var air = component.Air;
            var entryComponent = EntityManager.GetComponent<DisposalEntryComponent>(entry);

            if (_atmosSystem.GetTileMixture(EntityManager.GetComponent<TransformComponent>(component.Owner).Coordinates, true) is {Temperature: > 0} environment)
            {
                var transferMoles = 0.1f * (0.05f * Atmospherics.OneAtmosphere * 1.01f - air.Pressure) * air.Volume / (environment.Temperature * Atmospherics.R);

                component.Air = environment.Remove(transferMoles);
            }

            entryComponent.TryInsert(component);

            component.AutomaticEngageToken?.Cancel();
            component.AutomaticEngageToken = null;

            component.Pressure = 0;

            component.Engaged = false;

            HandleStateChange(component, true);
            UpdateVisualState(component, true);
            UpdateInterface(component, component.Powered);

            return true;
        }

        public void UpdateInterface(DisposalUnitComponent component, bool powered)
        {
            var stateString = Loc.GetString($"{component.State}");
            var state = new SharedDisposalUnitComponent.DisposalUnitBoundUserInterfaceState(EntityManager.GetComponent<MetaDataComponent>(component.Owner).EntityName, stateString, EstimatedFullPressure(component), powered, component.Engaged);
            component.UserInterface?.SetState(state);
        }

        private TimeSpan EstimatedFullPressure(DisposalUnitComponent component)
        {
            if (component.State == SharedDisposalUnitComponent.PressureState.Ready) return TimeSpan.Zero;

            var currentTime = GameTiming.CurTime;
            var pressure = component.Pressure;

            return TimeSpan.FromSeconds(currentTime.TotalSeconds + (1.0f - pressure) / PressurePerSecond);
        }

        public void UpdateVisualState(DisposalUnitComponent component)
        {
            UpdateVisualState(component, false);
        }

        public void UpdateVisualState(DisposalUnitComponent component, bool flush)
        {
            if (!EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearance))
            {
                return;
            }

            if (!EntityManager.GetComponent<TransformComponent>(component.Owner).Anchored)
            {
                appearance.SetData(SharedDisposalUnitComponent.Visuals.VisualState, SharedDisposalUnitComponent.VisualState.UnAnchored);
                appearance.SetData(SharedDisposalUnitComponent.Visuals.Handle, SharedDisposalUnitComponent.HandleState.Normal);
                appearance.SetData(SharedDisposalUnitComponent.Visuals.Light, SharedDisposalUnitComponent.LightState.Off);
                return;
            }

            appearance.SetData(SharedDisposalUnitComponent.Visuals.VisualState, component.Pressure < 1 ? SharedDisposalUnitComponent.VisualState.Charging : SharedDisposalUnitComponent.VisualState.Anchored);

            appearance.SetData(SharedDisposalUnitComponent.Visuals.Handle, component.Engaged
                ? SharedDisposalUnitComponent.HandleState.Engaged
                : SharedDisposalUnitComponent.HandleState.Normal);

            if (!component.Powered)
            {
                appearance.SetData(SharedDisposalUnitComponent.Visuals.Light, SharedDisposalUnitComponent.LightState.Off);
                return;
            }

            if (flush)
            {
                appearance.SetData(SharedDisposalUnitComponent.Visuals.VisualState, SharedDisposalUnitComponent.VisualState.Flushing);
                appearance.SetData(SharedDisposalUnitComponent.Visuals.Light, SharedDisposalUnitComponent.LightState.Off);
                return;
            }

            if (component.ContainedEntities.Count > 0)
            {
                appearance.SetData(SharedDisposalUnitComponent.Visuals.Light, SharedDisposalUnitComponent.LightState.Full);
                return;
            }

            appearance.SetData(SharedDisposalUnitComponent.Visuals.Light, component.Pressure < 1
                ? SharedDisposalUnitComponent.LightState.Charging
                : SharedDisposalUnitComponent.LightState.Ready);
        }

        public void Remove(DisposalUnitComponent component, EntityUid entity)
        {
            component.Container.Remove(entity);

            if (component.ContainedEntities.Count == 0)
            {
                component.AutomaticEngageToken?.Cancel();
                component.AutomaticEngageToken = null;
            }

            if (!component.RecentlyEjected.Contains(entity))
                component.RecentlyEjected.Add(entity);

            component.Dirty();
            HandleStateChange(component, true);
            UpdateVisualState(component);
        }

        public bool CanFlush(DisposalUnitComponent component)
        {
            return component.State == SharedDisposalUnitComponent.PressureState.Ready && component.Powered && EntityManager.GetComponent<TransformComponent>(component.Owner).Anchored;
        }

        public void Engage(DisposalUnitComponent component)
        {
            component.Engaged = true;
            UpdateVisualState(component);
            UpdateInterface(component, component.Powered);

            if (CanFlush(component))
            {
                component.Owner.SpawnTimer(component.FlushDelay, () => TryFlush(component));
            }
        }

        public void Disengage(DisposalUnitComponent component)
        {
            component.Engaged = false;
            UpdateVisualState(component);
            UpdateInterface(component, component.Powered);
        }

        /// <summary>
        /// Remove all entities currently in the disposal unit.
        /// </summary>
        public void TryEjectContents(DisposalUnitComponent component)
        {
            foreach (var entity in component.Container.ContainedEntities.ToArray())
            {
                Remove(component, entity);
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
        public void TryQueueEngage(DisposalUnitComponent component)
        {
            if (component.Deleted || !component.Powered && component.ContainedEntities.Count == 0)
            {
                return;
            }

            component.AutomaticEngageToken = new CancellationTokenSource();

            component.Owner.SpawnTimer(component._automaticEngageTime, () =>
            {
                if (!TryFlush(component))
                {
                    TryQueueEngage(component);
                }
            }, component.AutomaticEngageToken.Token);
        }

        public void AfterInsert(DisposalUnitComponent component, EntityUid entity)
        {
            TryQueueEngage(component);

            if (EntityManager.TryGetComponent(entity, out ActorComponent? actor))
            {
                component.UserInterface?.Close(actor.PlayerSession);
            }

            UpdateVisualState(component);
        }
    }
}
