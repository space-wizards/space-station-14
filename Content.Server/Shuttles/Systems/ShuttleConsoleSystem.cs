using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Events;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems
{
    public sealed class ShuttleConsoleSystem : SharedShuttleConsoleSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly TagSystem _tags = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ShuttleConsoleComponent, ComponentShutdown>(OnConsoleShutdown);
            SubscribeLocalEvent<ShuttleConsoleComponent, PowerChangedEvent>(OnConsolePowerChange);
            SubscribeLocalEvent<ShuttleConsoleComponent, AnchorStateChangedEvent>(OnConsoleAnchorChange);
            SubscribeLocalEvent<ShuttleConsoleComponent, ActivatableUIOpenAttemptEvent>(OnConsoleUIOpenAttempt);
            SubscribeNetworkEvent<ShuttleModeRequestEvent>(OnModeRequest);
            SubscribeLocalEvent<ShuttleConsoleComponent, BoundUIClosedEvent>(OnConsoleUIClose);

            SubscribeLocalEvent<PilotComponent, MoveEvent>(HandlePilotMove);
            SubscribeLocalEvent<PilotComponent, ComponentGetState>(OnGetState);
        }

        private void OnConsoleUIClose(EntityUid uid, ShuttleConsoleComponent component, BoundUIClosedEvent args)
        {
            if ((ShuttleConsoleUiKey) args.UiKey != ShuttleConsoleUiKey.Key ||
                args.Session.AttachedEntity is not {} user) return;
            RemovePilot(user);
        }

        private void OnConsoleUIOpenAttempt(EntityUid uid, ShuttleConsoleComponent component, ActivatableUIOpenAttemptEvent args)
        {
            if (!TryPilot(args.User, uid))
                args.Cancel();
        }

        private void OnConsoleAnchorChange(EntityUid uid, ShuttleConsoleComponent component, ref AnchorStateChangedEvent args)
        {
            UpdateState(component);
        }

        private void OnConsolePowerChange(EntityUid uid, ShuttleConsoleComponent component, PowerChangedEvent args)
        {
            UpdateState(component);
        }

        private bool TryPilot(EntityUid user, EntityUid uid)
        {
            if (!_tags.HasTag(user, "CanPilot") ||
                !TryComp<ShuttleConsoleComponent>(uid, out var component) ||
                !this.IsPowered(uid, EntityManager) ||
                !Transform(uid).Anchored ||
                !_blocker.CanInteract(user, uid))
            {
                return false;
            }

            var pilotComponent = EntityManager.EnsureComponent<PilotComponent>(user);
            var console = pilotComponent.Console;

            if (console != null)
            {
                RemovePilot(pilotComponent);

                if (console == component)
                {
                    return false;
                }
            }

            AddPilot(user, component);
            return true;
        }

        private void OnGetState(EntityUid uid, PilotComponent component, ref ComponentGetState args)
        {
            args.State = new PilotComponentState(component.Console?.Owner);
        }

        private void OnModeRequest(ShuttleModeRequestEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not { } player ||
                !TryComp<PilotComponent>(player, out var pilot) ||
                !TryComp<TransformComponent>(player, out var xform) ||
                pilot.Console is not ShuttleConsoleComponent console) return;

            if (!console.SubscribedPilots.Contains(pilot) ||
                !TryComp<ShuttleComponent>(xform.GridEntityId, out var shuttle)) return;

            SetShuttleMode(msg.Mode, console, shuttle);
        }

        /// <summary>
        /// Sets the shuttle's movement mode. Does minimal revalidation.
        /// </summary>
        private void SetShuttleMode(ShuttleMode mode, ShuttleConsoleComponent consoleComponent,
            ShuttleComponent shuttleComponent, TransformComponent? consoleXform = null)
        {
            // Re-validate
            if (!this.IsPowered(consoleComponent.Owner, EntityManager) ||
                !Resolve(consoleComponent.Owner, ref consoleXform) ||
                !consoleXform.Anchored ||
                consoleXform.GridID != Transform(shuttleComponent.Owner).GridID)
            {
                UpdateState(consoleComponent);
                return;
            }

            shuttleComponent.Mode = mode;

            switch (mode)
            {
                case ShuttleMode.Strafing:
                    break;
                case ShuttleMode.Cruise:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            UpdateState(consoleComponent);
        }

        /// <summary>
        /// Returns the position and angle of all dockingcomponents.
        /// </summary>
        private List<(EntityCoordinates Coordinates, Angle Angle)> GetAllDocks()
        {
            // TODO: NEED TO MAKE SURE THIS UPDATES ON ANCHORING CHANGES!
            var result = new List<(EntityCoordinates Coordinates, Angle Angle)>();

            foreach (var (comp, xform) in EntityQuery<DockingComponent, TransformComponent>(true))
            {
                if (xform.ParentUid != xform.GridUid) continue;

                result.Add((xform.Coordinates, xform.LocalRotation));
            }

            return result;
        }

        private void UpdateState(ShuttleConsoleComponent component)
        {
            TryComp<RadarConsoleComponent>(component.Owner, out var radar);
            var range = radar?.Range ?? 0f;

            TryComp<ShuttleComponent>(Transform(component.Owner).GridUid, out var shuttle);
            var mode = shuttle?.Mode ?? ShuttleMode.Cruise;

            var docks = GetAllDocks();

            _ui.GetUiOrNull(component.Owner, ShuttleConsoleUiKey.Key)
                ?.SetState(new ShuttleConsoleBoundInterfaceState(
                    mode,
                    range,
                    component.Owner,
                    docks));
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

        protected override void HandlePilotShutdown(EntityUid uid, PilotComponent component, ComponentShutdown args)
        {
            base.HandlePilotShutdown(uid, component, args);
            RemovePilot(component);
        }

        private void OnConsoleShutdown(EntityUid uid, ShuttleConsoleComponent component, ComponentShutdown args)
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

            pilotComponent.Console = component;
            ActionBlockerSystem.UpdateCanMove(entity);
            pilotComponent.Position = EntityManager.GetComponent<TransformComponent>(entity).Coordinates;
            Dirty(pilotComponent);
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
