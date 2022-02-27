using System;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Shuttles.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Shuttles;
using Content.Shared.Shuttles.Components;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.EntitySystems
{
    internal sealed class ShuttleConsoleSystem : SharedShuttleConsoleSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly TagSystem _tags = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ShuttleConsoleComponent, ComponentShutdown>(HandleConsoleShutdown);
            SubscribeLocalEvent<ShuttleConsoleComponent, ActivateInWorldEvent>(HandleConsoleInteract);
            SubscribeLocalEvent<ShuttleConsoleComponent, PowerChangedEvent>(HandlePowerChange);
            SubscribeLocalEvent<ShuttleConsoleComponent, GetVerbsEvent<InteractionVerb>>(OnConsoleInteract);

            SubscribeLocalEvent<PilotComponent, ComponentShutdown>(HandlePilotShutdown);
            SubscribeLocalEvent<PilotComponent, MoveEvent>(HandlePilotMove);
        }

        private void OnConsoleInteract(EntityUid uid, ShuttleConsoleComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            if (!args.CanAccess ||
                !args.CanInteract)
                return;

            var xform = EntityManager.GetComponent<TransformComponent>(uid);

            // Maybe move mode onto the console instead?
            if (!_mapManager.TryGetGrid(xform.GridID, out var grid) ||
                !EntityManager.TryGetComponent(grid.GridEntityId, out ShuttleComponent? shuttle)) return;

            InteractionVerb verb = new()
            {
                Text = Loc.GetString("shuttle-mode-toggle"),
                Act = () => ToggleShuttleMode(args.User, component, shuttle),
                Disabled = !xform.Anchored || EntityManager.TryGetComponent(uid, out ApcPowerReceiverComponent? receiver) && !receiver.Powered,
            };

            args.Verbs.Add(verb);
        }

        private void ToggleShuttleMode(EntityUid user, ShuttleConsoleComponent consoleComponent, ShuttleComponent shuttleComponent, TransformComponent? consoleXform = null)
        {
            // Re-validate
            if (EntityManager.TryGetComponent(consoleComponent.Owner, out ApcPowerReceiverComponent? receiver) && !receiver.Powered) return;

            if (!Resolve(consoleComponent.Owner, ref consoleXform)) return;

            if (!consoleXform.Anchored || consoleXform.GridID != EntityManager.GetComponent<TransformComponent>(shuttleComponent.Owner).GridID) return;

            switch (shuttleComponent.Mode)
            {
                case ShuttleMode.Cruise:
                    shuttleComponent.Mode = ShuttleMode.Docking;
                    _popup.PopupEntity(Loc.GetString("shuttle-mode-docking"), consoleComponent.Owner, Filter.Entities(user));
                    break;
                case ShuttleMode.Docking:
                    shuttleComponent.Mode = ShuttleMode.Cruise;
                    _popup.PopupEntity(Loc.GetString("shuttle-mode-cruise"), consoleComponent.Owner, Filter.Entities(user));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var toRemove = new RemQueue<PilotComponent>();

            foreach (var comp in EntityManager.EntityQuery<PilotComponent>())
            {
                if (comp.Console == null) continue;

                if (!_blocker.CanInteract(comp.Owner, comp.Console.Owner))
                {
                    toRemove.Add(comp);
                }
            }

            foreach (var comp in toRemove)
            {
                RemovePilot(comp);
            }
        }

        /// <summary>
        /// Console requires power to operate.
        /// </summary>
        private void HandlePowerChange(EntityUid uid, ShuttleConsoleComponent component, PowerChangedEvent args)
        {
            if (!args.Powered)
            {
                component.Enabled = false;

                ClearPilots(component);
            }
            else
            {
                component.Enabled = true;
            }
        }

        /// <summary>
        /// If pilot is moved then we'll stop them from piloting.
        /// </summary>
        private void HandlePilotMove(EntityUid uid, PilotComponent component, ref MoveEvent args)
        {
            if (component.Console == null || component.Position == null)
            {
                DebugTools.Assert(component.Position == null && component.Console == null);
                EntityManager.RemoveComponent<PilotComponent>(uid);
                return;
            }

            if (args.NewPosition.TryDistance(EntityManager, component.Position.Value, out var distance) &&
                distance < PilotComponent.BreakDistance) return;

            RemovePilot(component);
        }

        /// <summary>
        /// For now pilots just interact with the console and can start piloting with wasd.
        /// </summary>
        private void HandleConsoleInteract(EntityUid uid, ShuttleConsoleComponent component, ActivateInWorldEvent args)
        {
            if (!_tags.HasTag(args.User, "CanPilot"))
            {
                return;
            }

            var pilotComponent = EntityManager.EnsureComponent<PilotComponent>(args.User);

            if (!component.Enabled)
            {
                args.User.PopupMessage($"Console is not powered.");
                return;
            }

            args.Handled = true;
            var console = pilotComponent.Console;

            if (console != null)
            {
                RemovePilot(pilotComponent);

                if (console == component)
                {
                    return;
                }
            }

            AddPilot(args.User, component);
        }

        private void HandlePilotShutdown(EntityUid uid, PilotComponent component, ComponentShutdown args)
        {
            RemovePilot(component);
        }

        private void HandleConsoleShutdown(EntityUid uid, ShuttleConsoleComponent component, ComponentShutdown args)
        {
            ClearPilots(component);
        }

        public void AddPilot(EntityUid entity, ShuttleConsoleComponent component)
        {
            if (!EntityManager.TryGetComponent(entity, out PilotComponent? pilotComponent) ||
                component.SubscribedPilots.Contains(pilotComponent))
            {
                return;
            }

            if (TryComp<SharedEyeComponent>(entity, out var eye))
            {
                eye.Zoom = component.Zoom;
            }

            component.SubscribedPilots.Add(pilotComponent);

            _alertsSystem.ShowAlert(entity, AlertType.PilotingShuttle);

            entity.PopupMessage(Loc.GetString("shuttle-pilot-start"));
            pilotComponent.Console = component;
            pilotComponent.Position = EntityManager.GetComponent<TransformComponent>(entity).Coordinates;
            pilotComponent.Dirty();
        }

        public void RemovePilot(PilotComponent pilotComponent)
        {
            var console = pilotComponent.Console;

            if (console is not ShuttleConsoleComponent helmsman) return;

            pilotComponent.Console = null;
            pilotComponent.Position = null;

            if (TryComp<SharedEyeComponent>(pilotComponent.Owner, out var eye))
            {
                eye.Zoom = new(1.0f, 1.0f);
            }

            if (!helmsman.SubscribedPilots.Remove(pilotComponent)) return;

            _alertsSystem.ClearAlert(pilotComponent.Owner, AlertType.PilotingShuttle);

            pilotComponent.Owner.PopupMessage(Loc.GetString("shuttle-pilot-end"));

            if (pilotComponent.LifeStage < ComponentLifeStage.Stopping)
                EntityManager.RemoveComponent<PilotComponent>(pilotComponent.Owner);
        }

        public void RemovePilot(EntityUid entity)
        {
            if (!EntityManager.TryGetComponent(entity, out PilotComponent? pilotComponent)) return;

            RemovePilot(pilotComponent);
        }

        public void ClearPilots(ShuttleConsoleComponent component)
        {
            while (component.SubscribedPilots.TryGetValue(0, out var pilot))
            {
                RemovePilot(pilot);
            }
        }
    }
}
