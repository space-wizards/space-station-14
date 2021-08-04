using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Construction.Components;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Hands.Components;
using Content.Server.Power.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Atmos;
using Content.Shared.Disposal;
using Content.Shared.Disposal.Components;
using Content.Shared.Interaction;
using Content.Shared.Movement;
using Content.Shared.Notification.Managers;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Disposal.Unit.EntitySystems
{
    public sealed class DisposalUnitSystem : SharedDisposalUnitSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;

        private List<DisposalUnitComponent> _activeDisposals = new();

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

            SubscribeLocalEvent<DisposalUnitComponent, ThrowCollideEvent>(HandleThrowCollide);

            // Interactions
            SubscribeLocalEvent<DisposalUnitComponent, ActivateInWorldEvent>(HandleActivate);
            SubscribeLocalEvent<DisposalUnitComponent, InteractHandEvent>(HandleInteractHand);
            SubscribeLocalEvent<DisposalUnitComponent, InteractUsingEvent>(HandleInteractUsing);
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
            if (!ComponentManager.TryGetComponent(component.Owner.Uid, out ApcPowerReceiverComponent? receiver))
            {
                return;
            }

            receiver.PowerDisabled = !receiver.PowerDisabled;
            UpdateInterface(component, receiver.Powered);
        }
        #endregion

        public void Engage(DisposalUnitComponent component)
        {
            component.Engaged = true;
            component.UpdateVisualState();
            UpdateInterface(component, component.Powered);

            if (component.CanFlush())
            {
                component.Owner.SpawnTimer(component.FlushDelay, () => TryFlush(component));
            }
        }

        public void Disengage(DisposalUnitComponent component)
        {
            component.Engaged = false;
            component.UpdateVisualState();
            UpdateInterface(component, component.Powered);
        }

        #region Eventbus Handlers
        private void HandleActivate(EntityUid uid, DisposalUnitComponent component, ActivateInWorldEvent args)
        {
            if (!args.User.TryGetComponent(out ActorComponent? actor))
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
            if (!args.User.TryGetComponent(out ActorComponent? actor)) return;

            // Duplicated code here, not sure how else to get actor inside to make UserInterface happy.

            if (!IsValidInteraction(args)) return;
            component.UserInterface?.Open(actor.PlayerSession);
            args.Handled = true;
        }

        private void HandleInteractUsing(EntityUid uid, DisposalUnitComponent component, InteractUsingEvent args)
        {
            var result = component.TryDrop(args.User, args.Used);

            if (result)
                args.Handled = true;
        }

        private void HandleThrowCollide(EntityUid uid, DisposalUnitComponent component, ThrowCollideEvent args)
        {
            if (!component.CanInsert(args.Thrown) ||
                _robustRandom.NextDouble() > 0.75 ||
                !component.Container.Insert(args.Thrown))
            {
                return;
            }

            component.AfterInsert(args.Thrown);
        }

        private void HandleDisposalInit(EntityUid uid, DisposalUnitComponent component, ComponentInit args)
        {
            component.Container = component.Owner.EnsureContainer<Container>(component.Name);

            if (component.UserInterface != null)
            {
                component.UserInterface.OnReceiveMessage += component.OnUiReceiveMessage;
            }

            UpdateInterface(component, component.Powered);

            if (!component.Owner.HasComponent<AnchorableComponent>())
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
            // TODO: Need to check the other stuff.
            if (!args.Powered)
            {
                component.AutomaticEngageToken?.Cancel();
                component.AutomaticEngageToken = null;
            }

            HandleStateChange(component, args.Powered && component.State == SharedDisposalUnitComponent.PressureState.Pressurizing);
            component.UpdateVisualState();
            UpdateInterface(component, args.Powered);

            if (component.Engaged && !TryFlush(component))
            {
                component.TryQueueEngage();
            }
        }

        /// <summary>
        /// Add or remove this disposal from the active ones for updating.
        /// </summary>
        private void HandleStateChange(DisposalUnitComponent component, bool active)
        {
            if (active)
            {
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

            if (!args.Entity.TryGetComponent(out HandsComponent? hands) ||
                hands.Count == 0 ||
                currentTime < component.LastExitAttempt + ExitAttemptDelay)
            {
                return;
            }

            component.LastExitAttempt = currentTime;
            component.Remove(args.Entity);
        }

        private static void OnAnchored(EntityUid uid, DisposalUnitComponent component, AnchoredEvent args)
        {
            component.UpdateVisualState();
        }

        private static void OnUnanchored(EntityUid uid, DisposalUnitComponent component, UnanchoredEvent args)
        {
            component.UpdateVisualState();
            component.TryEjectContents();
        }
        #endregion

        private bool Update(DisposalUnitComponent component, float frameTime)
        {
            var oldPressure = component.Pressure;

            component.Pressure = MathF.Min(1.0f, component.Pressure + PressurePerSecond * frameTime);

            var state = component.State;

            if (oldPressure < 1 && state == SharedDisposalUnitComponent.PressureState.Ready)
            {
                component.UpdateVisualState();

                if (component.Engaged)
                {
                    TryFlush(component);
                }
            }

            return state == SharedDisposalUnitComponent.PressureState.Ready;
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

            if (!eventArgs.User.HasComponent<IHandsComponent>())
            {
                eventArgs.Target.PopupMessage(eventArgs.User, Loc.GetString("ui-disposal-unit-is-valid-interaction-no-hands"));
                return false;
            }

            return true;
        }

        public bool TryFlush(DisposalUnitComponent component)
        {
            if (component.Deleted || !component.CanFlush())
            {
                return false;
            }

            var grid = _mapManager.GetGrid(component.Owner.Transform.GridID);
            var coords = component.Owner.Transform.Coordinates;
            var entry = grid.GetLocal(coords)
                .FirstOrDefault(entity => EntityManager.ComponentManager.HasComponent<DisposalEntryComponent>(entity));

            if (entry == default)
            {
                return false;
            }

            var air = component.Air;
            var entryComponent = EntityManager.ComponentManager.GetComponent<DisposalEntryComponent>(entry);

            if (_atmosSystem.GetTileMixture(component.Owner.Transform.Coordinates, true) is {Temperature: > 0} environment)
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
            component.UpdateVisualState(true);
            UpdateInterface(component, component.Powered);

            return true;
        }

        public void UpdateInterface(DisposalUnitComponent component, bool powered)
        {
            var stateString = Loc.GetString($"{component.State}");
            var state = new SharedDisposalUnitComponent.DisposalUnitBoundUserInterfaceState(component.Owner.Name, stateString, EstimatedFullPressure(component), powered, component.Engaged);
            component.UserInterface?.SetState(state);
        }

        private TimeSpan EstimatedFullPressure(DisposalUnitComponent component)
        {
            if (component.State == SharedDisposalUnitComponent.PressureState.Ready) return TimeSpan.Zero;

            var currentTime = GameTiming.CurTime;
            var pressure = component.Pressure;

            return TimeSpan.FromSeconds(currentTime.TotalSeconds + (1.0f - pressure) / PressurePerSecond);
        }
    }
}
