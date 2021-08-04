using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Construction.Components;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Hands.Components;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Disposal;
using Content.Shared.Interaction;
using Content.Shared.Movement;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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
            SubscribeLocalEvent<DisposalUnitComponent, ComponentInit>(HandleDisposalInit);
            SubscribeLocalEvent<DisposalUnitComponent, ThrowCollideEvent>(HandleThrowCollide);
            SubscribeLocalEvent<DisposalUnitComponent, ActivateInWorldEvent>(HandleActivate);
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

        #region Eventbus Handlers
        private void HandleActivate(EntityUid uid, DisposalUnitComponent component, ActivateInWorldEvent args)
        {
            if (!args.User.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }

            args.Handled = true;

            if (component.IsValidInteraction(args))
            {
                component.UserInterface?.Open(actor.PlayerSession);
            }
        }

        private void HandleThrowCollide(EntityUid uid, DisposalUnitComponent component, ThrowCollideEvent args)
        {
            if (!component.CanInsert(args.Thrown) ||
                _robustRandom.NextDouble() > 0.75 ||
                !component._container.Insert(args.Thrown))
            {
                return;
            }

            component.AfterInsert(args.Thrown);
        }

        private void HandleDisposalInit(EntityUid uid, DisposalUnitComponent component, ComponentInit args)
        {
            component._container = component.Owner.EnsureContainer<Container>(component.Name);

            if (component.UserInterface != null)
            {
                component.UserInterface.OnReceiveMessage += component.OnUiReceiveMessage;
            }

            component.UpdateInterface(component.Powered);
        }

        private void HandlePowerChange(EntityUid uid, DisposalUnitComponent component, PowerChangedEvent args)
        {
            // TODO: Need to check the other stuff.
            if (!args.Powered)
            {
                component._automaticEngageToken?.Cancel();
                component._automaticEngageToken = null;
            }

            HandleStateChange(component, args.Powered && component._pressure < 1.0f);
            component.UpdateVisualState();
            component.UpdateInterface(args.Powered);

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
            var oldPressure = component._pressure;

            component._pressure = component._pressure + frameTime > 1
                ? 1
                : component._pressure + 0.05f * frameTime;

            if (oldPressure < 1 && component._pressure >= 1)
            {
                component.UpdateVisualState();

                if (component.Engaged)
                {
                    TryFlush(component);
                }
            }

            // TODO: Ideally we'd just send the start and end and client could lerp as the bandwidth would be way lower
            if (component._pressure < 1.0f || oldPressure < 1.0f && component._pressure >= 1.0f)
            {
                // Should be powered still so no need to check.
                component.UpdateInterface(true);
            }

            if (component._pressure >= 1.0f)
            {
                return true;
            }

            return false;
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

            component._automaticEngageToken?.Cancel();
            component._automaticEngageToken = null;

            component._pressure = 0;

            component.Engaged = false;

            HandleStateChange(component, true);
            component.UpdateVisualState(true);
            component.UpdateInterface(component.Powered);

            return true;
        }
    }
}
