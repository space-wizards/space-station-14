using Content.Server.Alert;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Interaction;
using Content.Shared.Notification.Managers;
using Content.Shared.Shuttles;
using Robust.Shared.GameObjects;

namespace Content.Server.Shuttles
{
    internal sealed class HelmsmanSystem : SharedHelmsmanSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HelmsmanConsoleComponent, ComponentShutdown>(HandleHelmsmanShutdown);
            SubscribeLocalEvent<PilotComponent, ComponentShutdown>(HandlePilotShutdown);
            SubscribeLocalEvent<HelmsmanConsoleComponent, ActivateInWorldEvent>(HandleHelmsmanInteract);
            SubscribeLocalEvent<PilotComponent, MoveEvent>(HandlePilotMove);
        }

        private void HandlePilotMove(EntityUid uid, PilotComponent component, MoveEvent args)
        {
            if (component.Console == null) return;
            RemovePilot(component);
        }

        private void HandleHelmsmanInteract(EntityUid uid, HelmsmanConsoleComponent component, ActivateInWorldEvent args)
        {
            if (!args.User.TryGetComponent(out PilotComponent? pilotComponent))
            {
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

        private void HandleHelmsmanShutdown(EntityUid uid, HelmsmanConsoleComponent component, ComponentShutdown args)
        {
            ClearPilots(component);
        }

        public void AddPilot(IEntity entity, HelmsmanConsoleComponent component)
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

            // TODO: LOC.
            entity.PopupMessage("Piloting ship");
            pilotComponent.Console = component;
            pilotComponent.Dirty();
            //ComponentManager.AddComponent<ShuttleControllerComponent>(entity);
        }

        public void RemovePilot(PilotComponent pilotComponent)
        {
            var console = pilotComponent.Console;

            if (console is not HelmsmanConsoleComponent helmsman)
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
            pilotComponent.Owner.PopupMessage("Stopped piloting");
            //ComponentManager.RemoveComponent<ShuttleControllerComponent>(pilotComponent.Owner.Uid);
        }

        public void RemovePilot(IEntity entity)
        {
            if (!entity.TryGetComponent(out PilotComponent? pilotComponent)) return;

            RemovePilot(pilotComponent);
        }

        public void ClearPilots(HelmsmanConsoleComponent component)
        {
            foreach (var pilot in component.SubscribedPilots)
            {
                RemovePilot(pilot);
            }
        }
    }
}
