using Content.Server.Alert;
using Content.Server.Power.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Interaction;
using Content.Shared.Notification.Managers;
using Content.Shared.Shuttles;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Shuttles
{
    internal sealed class ShuttleConsoleSystem : SharedHelmsmanSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ShuttleConsoleComponent, ComponentShutdown>(HandleHelmsmanShutdown);
            SubscribeLocalEvent<PilotComponent, ComponentShutdown>(HandlePilotShutdown);
            SubscribeLocalEvent<ShuttleConsoleComponent, ActivateInWorldEvent>(HandleHelmsmanInteract);
            SubscribeLocalEvent<PilotComponent, MoveEvent>(HandlePilotMove);
            SubscribeLocalEvent<ShuttleConsoleComponent, PowerChangedEvent>(HandlePowerChange);
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
        private void HandlePilotMove(EntityUid uid, PilotComponent component, MoveEvent args)
        {
            if (component.Console == null) return;
            RemovePilot(component);
        }

        /// <summary>
        /// For now pilots just interact with the console and can start piloting with wasd.
        /// </summary>
        private void HandleHelmsmanInteract(EntityUid uid, ShuttleConsoleComponent component, ActivateInWorldEvent args)
        {
            if (!args.User.TryGetComponent(out PilotComponent? pilotComponent))
            {
                return;
            }

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

                if (console != component)
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

        private void HandleHelmsmanShutdown(EntityUid uid, ShuttleConsoleComponent component, ComponentShutdown args)
        {
            ClearPilots(component);
        }

        public void AddPilot(IEntity entity, ShuttleConsoleComponent component)
        {
            if (!Get<ActionBlockerSystem>().CanInteract(entity) ||
                !entity.TryGetComponent(out PilotComponent? pilotComponent) ||
                component.SubscribedPilots.Contains(pilotComponent))
            {
                return;
            }

            component.SubscribedPilots.Add(pilotComponent);

            if (entity.TryGetComponent(out ServerAlertsComponent? alertsComponent))
            {
                alertsComponent.ShowAlert(AlertType.PilotingShuttle);
            }

            entity.PopupMessage(Loc.GetString("shuttle-pilot-start"));
            pilotComponent.Console = component;
            pilotComponent.Dirty();
        }

        public void RemovePilot(PilotComponent pilotComponent)
        {
            var console = pilotComponent.Console;

            if (console is not ShuttleConsoleComponent helmsman)
            {
                return;
            }

            pilotComponent.Console = null;

            if (!helmsman.SubscribedPilots.Remove(pilotComponent))
            {
                return;
            }

            if (pilotComponent.Owner.TryGetComponent(out ServerAlertsComponent? alertsComponent))
            {
                alertsComponent.ClearAlert(AlertType.PilotingShuttle);
            }

            pilotComponent.Dirty();
            pilotComponent.Owner.PopupMessage(Loc.GetString("shuttle-pilot-end"));
        }

        public void RemovePilot(IEntity entity)
        {
            if (!entity.TryGetComponent(out PilotComponent? pilotComponent)) return;

            RemovePilot(pilotComponent);
        }

        public void ClearPilots(ShuttleConsoleComponent component)
        {
            foreach (var pilot in component.SubscribedPilots)
            {
                RemovePilot(pilot);
            }
        }
    }
}
