using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Popups;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Events;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems
{
    public sealed class ShuttleConsoleSystem : SharedShuttleConsoleSystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly ShuttleSystem _shuttle = default!;
        [Dependency] private readonly TagSystem _tags = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ShuttleConsoleComponent, ComponentShutdown>(OnConsoleShutdown);
            SubscribeLocalEvent<ShuttleConsoleComponent, PowerChangedEvent>(OnConsolePowerChange);
            SubscribeLocalEvent<ShuttleConsoleComponent, AnchorStateChangedEvent>(OnConsoleAnchorChange);
            SubscribeLocalEvent<ShuttleConsoleComponent, ActivatableUIOpenAttemptEvent>(OnConsoleUIOpenAttempt);
            SubscribeLocalEvent<ShuttleConsoleComponent, ShuttleConsoleDestinationMessage>(OnDestinationMessage);
            SubscribeLocalEvent<ShuttleConsoleComponent, BoundUIClosedEvent>(OnConsoleUIClose);

            SubscribeLocalEvent<DockEvent>(OnDock);
            SubscribeLocalEvent<UndockEvent>(OnUndock);

            SubscribeLocalEvent<PilotComponent, MoveEvent>(HandlePilotMove);
            SubscribeLocalEvent<PilotComponent, ComponentGetState>(OnGetState);
        }

        private void OnDestinationMessage(EntityUid uid, ShuttleConsoleComponent component, ShuttleConsoleDestinationMessage args)
        {
            if (!TryComp<FTLDestinationComponent>(args.Destination, out var dest)) return;

            if (!dest.Enabled) return;

            EntityUid? entity = component.Owner;

            var getShuttleEv = new ConsoleShuttleEvent
            {
                Console = uid,
            };

            RaiseLocalEvent(entity.Value, ref getShuttleEv);
            entity = getShuttleEv.Console;

            if (entity == null || dest.Whitelist?.IsValid(entity.Value, EntityManager) == false) return;

            if (!TryComp<TransformComponent>(entity, out var xform) ||
                !TryComp<ShuttleComponent>(xform.GridUid, out var shuttle)) return;

            if (HasComp<FTLComponent>(xform.GridUid))
            {
                _popup.PopupCursor(Loc.GetString("shuttle-console-in-ftl"), args.Session);
                return;
            }

            if (!_shuttle.CanFTL(shuttle.Owner, out var reason))
            {
                _popup.PopupCursor(reason, args.Session);
                return;
            }

            _shuttle.FTLTravel(shuttle, args.Destination, hyperspaceTime: _shuttle.TransitTime);
        }

        private void OnDock(DockEvent ev)
        {
            RefreshShuttleConsoles();
        }

        private void OnUndock(UndockEvent ev)
        {
            RefreshShuttleConsoles();
        }

        public void RefreshShuttleConsoles(EntityUid uid)
        {
            // TODO: Should really call this per shuttle in some instances.
            RefreshShuttleConsoles();
        }

        /// <summary>
        /// Refreshes all of the data for shuttle consoles.
        /// </summary>
        public void RefreshShuttleConsoles()
        {
            var docks = GetAllDocks();

            foreach (var comp in EntityQuery<ShuttleConsoleComponent>(true))
            {
                UpdateState(comp, docks);
            }
        }

        /// <summary>
        /// Stop piloting if the window is closed.
        /// </summary>
        private void OnConsoleUIClose(EntityUid uid, ShuttleConsoleComponent component, BoundUIClosedEvent args)
        {
            if ((ShuttleConsoleUiKey) args.UiKey != ShuttleConsoleUiKey.Key ||
                args.Session.AttachedEntity is not {} user) return;

            // In case they D/C should still clean them up.
            foreach (var comp in EntityQuery<AutoDockComponent>(true))
            {
                comp.Requesters.Remove(user);
            }

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

        private void OnConsolePowerChange(EntityUid uid, ShuttleConsoleComponent component, ref PowerChangedEvent args)
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

        /// <summary>
        /// Returns the position and angle of all dockingcomponents.
        /// </summary>
        private List<DockingInterfaceState> GetAllDocks()
        {
            // TODO: NEED TO MAKE SURE THIS UPDATES ON ANCHORING CHANGES!
            var result = new List<DockingInterfaceState>();

            foreach (var (comp, xform) in EntityQuery<DockingComponent, TransformComponent>(true))
            {
                if (xform.ParentUid != xform.GridUid) continue;

                var state = new DockingInterfaceState()
                {
                    Coordinates = xform.Coordinates,
                    Angle = xform.LocalRotation,
                    Entity = comp.Owner,
                    Connected = comp.Docked,
                    Color = comp.RadarColor,
                    HighlightedColor = comp.HighlightedRadarColor,
                };
                result.Add(state);
            }

            return result;
        }

        private void UpdateState(ShuttleConsoleComponent component, List<DockingInterfaceState>? docks = null)
        {
            EntityUid? entity = component.Owner;

            var getShuttleEv = new ConsoleShuttleEvent
            {
                Console = entity,
            };

            RaiseLocalEvent(entity.Value, ref getShuttleEv);
            entity = getShuttleEv.Console;

            TryComp<TransformComponent>(entity, out var consoleXform);
            TryComp<RadarConsoleComponent>(entity, out var radar);
            var range = radar?.MaxRange ?? 0f;

            TryComp<ShuttleComponent>(consoleXform?.GridUid, out var shuttle);

            var destinations = new List<(EntityUid, string, bool)>();
            var ftlState = FTLState.Available;
            var ftlTime = TimeSpan.Zero;

            if (TryComp<FTLComponent>(shuttle?.Owner, out var shuttleFtl))
            {
                ftlState = shuttleFtl.State;
                ftlTime = _timing.CurTime + TimeSpan.FromSeconds(shuttleFtl.Accumulator);
            }

            // Mass too large
            if (entity != null && shuttle?.Owner != null && (!TryComp<PhysicsComponent>(shuttle?.Owner, out var shuttleBody) ||
                shuttleBody.Mass < 1000f))
            {
                var metaQuery = GetEntityQuery<MetaDataComponent>();

                // Can't go anywhere when in FTL.
                var locked = shuttleFtl != null || Paused(shuttle!.Owner);

                // Can't cache it because it may have a whitelist for the particular console.
                // Include paused as we still want to show CentCom.
                foreach (var comp in EntityQuery<FTLDestinationComponent>(true))
                {
                    // Can't warp to itself or if it's not on the whitelist.
                    if (comp.Owner == shuttle?.Owner ||
                        comp.Whitelist?.IsValid(entity.Value) == false) continue;

                    var meta = metaQuery.GetComponent(comp.Owner);
                    var name = meta.EntityName;

                    if (string.IsNullOrEmpty(name))
                        name = Loc.GetString("shuttle-console-unknown");

                    var canTravel = !locked &&
                                    comp.Enabled &&
                                    !Paused(comp.Owner, meta) &&
                                    (!TryComp<FTLComponent>(comp.Owner, out var ftl) || ftl.State == FTLState.Cooldown);

                    // Can't travel to same map.
                    if (canTravel && consoleXform?.MapUid == Transform(comp.Owner).MapUid)
                    {
                        canTravel = false;
                    }

                    destinations.Add((comp.Owner, name, canTravel));
                }
            }

            docks ??= GetAllDocks();

            _ui.GetUiOrNull(component.Owner, ShuttleConsoleUiKey.Key)
                ?.SetState(new ShuttleConsoleBoundInterfaceState(
                    ftlState,
                    ftlTime,
                    destinations,
                    range,
                    consoleXform?.Coordinates,
                    consoleXform?.LocalRotation,
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
