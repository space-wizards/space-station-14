using System.Numerics;
using Content.Server.Doors.Systems;
using Content.Server.NPC.Pathfinding;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Events;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems
{
    public sealed partial class DockingSystem : SharedDockingSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly DoorBoltSystem _bolts = default!;
        [Dependency] private readonly DoorSystem _doorSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly FixtureSystem _fixtureSystem = default!;
        [Dependency] private readonly PathfindingSystem _pathfinding = default!;
        [Dependency] private readonly ShuttleConsoleSystem _console = default!;
        [Dependency] private readonly SharedJointSystem _jointSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        private const string DockingJoint = "docking";
        private const float DockingRadius = 0.20f;

        private EntityQuery<MapGridComponent> _gridQuery;
        private EntityQuery<PhysicsComponent> _physicsQuery;
        private EntityQuery<TransformComponent> _xformQuery;

        private HashSet<Entity<DockingComponent>> _dockingSet = new();
        private HashSet<Entity<DockingComponent, DoorBoltComponent>> _dockingBoltSet = new();

        public override void Initialize()
        {
            base.Initialize();
            _gridQuery = GetEntityQuery<MapGridComponent>();
            _physicsQuery = GetEntityQuery<PhysicsComponent>();
            _xformQuery = GetEntityQuery<TransformComponent>();

            SubscribeLocalEvent<DockingComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DockingComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<DockingComponent, AnchorStateChangedEvent>(OnAnchorChange);
            SubscribeLocalEvent<DockingComponent, ReAnchorEvent>(OnDockingReAnchor);

            SubscribeLocalEvent<DockingComponent, BeforeDoorAutoCloseEvent>(OnAutoClose);

            // Yes this isn't in shuttle console; it may be used by other systems technically.
            // in which case I would also add their subs here.
            SubscribeLocalEvent<ShuttleConsoleComponent, DockRequestMessage>(OnRequestDock);
            SubscribeLocalEvent<ShuttleConsoleComponent, UndockRequestMessage>(OnRequestUndock);
        }

        /// <summary>
        /// Sets the docks for the provided entity as enabled or disabled.
        /// </summary>
        public void SetDocks(EntityUid gridUid, bool enabled)
        {
            _dockingSet.Clear();
            _lookup.GetChildEntities(gridUid, _dockingSet);

            foreach (var dock in _dockingSet)
            {
                Undock(dock);
                dock.Comp.Enabled = enabled;
            }
        }

        public void SetDockBolts(EntityUid gridUid, bool enabled)
        {
            _dockingBoltSet.Clear();
            _lookup.GetChildEntities(gridUid, _dockingBoltSet);

            foreach (var entity in _dockingBoltSet)
            {
                _doorSystem.TryClose(entity);
                _bolts.SetBoltsWithAudio(entity.Owner, entity.Comp2, enabled);
            }
        }

        private void OnAutoClose(EntityUid uid, DockingComponent component, BeforeDoorAutoCloseEvent args)
        {
            // We'll just pin the door open when docked.
            if (component.Docked)
                args.Cancel();
        }

        private void OnShutdown(EntityUid uid, DockingComponent component, ComponentShutdown args)
        {
            if (component.DockedWith == null ||
                EntityManager.GetComponent<MetaDataComponent>(uid).EntityLifeStage > EntityLifeStage.MapInitialized)
            {
                return;
            }

            var gridUid = Transform(uid).GridUid;

            if (gridUid != null && !Terminating(gridUid.Value))
            {
                _console.RefreshShuttleConsoles();
            }

            Cleanup(uid, component);
        }

        private void Cleanup(EntityUid dockAUid, DockingComponent dockA)
        {
            _pathfinding.RemovePortal(dockA.PathfindHandle);

            if (dockA.DockJoint != null)
                _jointSystem.RemoveJoint(dockA.DockJoint);

            var dockBUid = dockA.DockedWith;

            if (dockBUid == null ||
                !TryComp(dockBUid, out DockingComponent? dockB))
            {
                DebugTools.Assert(false);
                Log.Error($"Tried to cleanup {dockAUid} but not docked?");

                dockA.DockedWith = null;
                if (dockA.DockJoint != null)
                {
                    // We'll still cleanup the dock joint on release at least
                    _jointSystem.RemoveJoint(dockA.DockJoint);
                }

                return;
            }

            dockB.DockedWith = null;
            dockB.DockJoint = null;
            dockB.DockJointId = null;

            dockA.DockJoint = null;
            dockA.DockedWith = null;
            dockA.DockJointId = null;

            // If these grids are ever null then need to look at fixing ordering for unanchored events elsewhere.
            var gridAUid = EntityManager.GetComponent<TransformComponent>(dockAUid).GridUid;
            var gridBUid = EntityManager.GetComponent<TransformComponent>(dockBUid.Value).GridUid;

            var msg = new UndockEvent
            {
                DockA = dockA,
                DockB = dockB,
                GridAUid = gridAUid!.Value,
                GridBUid = gridBUid!.Value,
            };

            RaiseLocalEvent(dockAUid, msg);
            RaiseLocalEvent(dockBUid.Value, msg);
            RaiseLocalEvent(msg);
        }

        private void OnStartup(Entity<DockingComponent> entity, ref ComponentStartup args)
        {
            var uid = entity.Owner;
            var component = entity.Comp;

            // Use startup so transform already initialized
            if (!EntityManager.GetComponent<TransformComponent>(uid).Anchored)
                return;

            SetDockingEnabled((uid, component), true);

            // This little gem is for docking deserialization
            if (component.DockedWith != null)
            {
                // They're still initialising so we'll just wait for both to be ready.
                if (MetaData(component.DockedWith.Value).EntityLifeStage < EntityLifeStage.Initialized)
                    return;

                var otherDock = EntityManager.GetComponent<DockingComponent>(component.DockedWith.Value);
                DebugTools.Assert(otherDock.DockedWith != null);

                Dock(uid, component, component.DockedWith.Value, otherDock);
                DebugTools.Assert(component.Docked && otherDock.Docked);
            }
        }

        private void OnAnchorChange(Entity<DockingComponent> entity, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
            {
                SetDockingEnabled(entity, true);
            }
            else
            {
                SetDockingEnabled(entity, false);
            }

            _console.RefreshShuttleConsoles();
        }

        private void OnDockingReAnchor(Entity<DockingComponent> entity, ref ReAnchorEvent args)
        {
            var uid = entity.Owner;
            var component = entity.Comp;

            if (!component.Docked)
                return;

            var otherDock = component.DockedWith;
            var other = Comp<DockingComponent>(otherDock!.Value);

            Undock(entity);
            Dock(uid, component, otherDock.Value, other);
            _console.RefreshShuttleConsoles();
        }

        public void SetDockingEnabled(Entity<DockingComponent> entity, bool value)
        {
            if (entity.Comp.Enabled == value)
                return;

            entity.Comp.Enabled = value;

            if (!entity.Comp.Enabled && entity.Comp.DockedWith != null)
            {
                Undock(entity);
            }
        }

        /// <summary>
        /// Docks 2 ports together and assumes it is valid.
        /// </summary>
        public void Dock(EntityUid dockAUid, DockingComponent dockA, EntityUid dockBUid, DockingComponent dockB)
        {
            if (dockBUid.GetHashCode() < dockAUid.GetHashCode())
            {
                (dockA, dockB) = (dockB, dockA);
                (dockAUid, dockBUid) = (dockBUid, dockAUid);
            }

            Log.Debug($"Docking between {dockAUid} and {dockBUid}");

            // https://gamedev.stackexchange.com/questions/98772/b2distancejoint-with-frequency-equal-to-0-vs-b2weldjoint

            // We could also potentially use a prismatic joint? Depending if we want clamps that can extend or whatever
            var dockAXform = EntityManager.GetComponent<TransformComponent>(dockAUid);
            var dockBXform = EntityManager.GetComponent<TransformComponent>(dockBUid);

            DebugTools.Assert(dockAXform.GridUid != null);
            DebugTools.Assert(dockBXform.GridUid != null);
            var gridA = dockAXform.GridUid!.Value;
            var gridB = dockBXform.GridUid!.Value;

            // May not be possible if map or the likes.
            if (HasComp<PhysicsComponent>(gridA) &&
                HasComp<PhysicsComponent>(gridB))
            {
                SharedJointSystem.LinearStiffness(
                    2f,
                    0.7f,
                    EntityManager.GetComponent<PhysicsComponent>(gridA).Mass,
                    EntityManager.GetComponent<PhysicsComponent>(gridB).Mass,
                    out var stiffness,
                    out var damping);

                // These need playing around with
                // Could also potentially have collideconnected false and stiffness 0 but it was a bit more suss???
                WeldJoint joint;

                // Pre-existing joint so use that.
                if (dockA.DockJointId != null)
                {
                    DebugTools.Assert(dockB.DockJointId == dockA.DockJointId);
                    joint = _jointSystem.GetOrCreateWeldJoint(gridA, gridB, dockA.DockJointId);
                }
                else
                {
                    joint = _jointSystem.GetOrCreateWeldJoint(gridA, gridB, DockingJoint + dockAUid);
                }

                var gridAXform = EntityManager.GetComponent<TransformComponent>(gridA);
                var gridBXform = EntityManager.GetComponent<TransformComponent>(gridB);

                var anchorA = dockAXform.LocalPosition + dockAXform.LocalRotation.ToWorldVec() / 2f;
                var anchorB = dockBXform.LocalPosition + dockBXform.LocalRotation.ToWorldVec() / 2f;

                joint.LocalAnchorA = anchorA;
                joint.LocalAnchorB = anchorB;
                joint.ReferenceAngle = (float) (_transform.GetWorldRotation(gridBXform) - _transform.GetWorldRotation(gridAXform));
                joint.CollideConnected = true;
                joint.Stiffness = stiffness;
                joint.Damping = damping;

                dockA.DockJoint = joint;
                dockA.DockJointId = joint.ID;

                dockB.DockJoint = joint;
                dockB.DockJointId = joint.ID;
            }

            dockA.DockedWith = dockBUid;
            dockB.DockedWith = dockAUid;

            if (TryComp(dockAUid, out DoorComponent? doorA))
            {
                if (_doorSystem.TryOpen(dockAUid, doorA))
                {
                    doorA.ChangeAirtight = false;
                    if (TryComp<DoorBoltComponent>(dockAUid, out var airlockA))
                    {
                        _bolts.SetBoltsWithAudio(dockAUid, airlockA, true);
                    }
                }
            }

            if (TryComp(dockBUid, out DoorComponent? doorB))
            {
                if (_doorSystem.TryOpen(dockBUid, doorB))
                {
                    doorB.ChangeAirtight = false;
                    if (TryComp<DoorBoltComponent>(dockBUid, out var airlockB))
                    {
                        _bolts.SetBoltsWithAudio(dockBUid, airlockB, true);
                    }
                }
            }

            if (_pathfinding.TryCreatePortal(dockAXform.Coordinates, dockBXform.Coordinates, out var handle))
            {
                dockA.PathfindHandle = handle;
                dockB.PathfindHandle = handle;
            }

            var msg = new DockEvent
            {
                DockA = dockA,
                DockB = dockB,
                GridAUid = gridA,
                GridBUid = gridB,
            };

            RaiseLocalEvent(dockAUid, msg);
            RaiseLocalEvent(dockBUid, msg);
            RaiseLocalEvent(msg);
        }

        private bool CanDock(EntityUid dockAUid, EntityUid dockBUid, DockingComponent dockA, DockingComponent dockB)
        {
            if (!dockA.Enabled ||
                !dockB.Enabled ||
                dockA.DockedWith != null ||
                dockB.DockedWith != null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Attempts to dock 2 ports together and will return early if it's not possible.
        /// </summary>
        private void TryDock(EntityUid dockAUid, DockingComponent dockA, Entity<DockingComponent> dockB)
        {
            if (!CanDock(dockAUid, dockB, dockA, dockB))
                return;

            Dock(dockAUid, dockA, dockB, dockB);
        }

        public void Undock(Entity<DockingComponent> dock)
        {
            if (dock.Comp.DockedWith == null)
                return;

            OnUndock(dock.Owner);
            OnUndock(dock.Comp.DockedWith.Value);
            Cleanup(dock.Owner, dock);
        }

        private void OnUndock(EntityUid dockUid)
        {
            if (TerminatingOrDeleted(dockUid))
                return;

            if (TryComp<DoorBoltComponent>(dockUid, out var airlock))
                _bolts.SetBoltsWithAudio(dockUid, airlock, false);

            if (TryComp(dockUid, out DoorComponent? door) && _doorSystem.TryClose(dockUid, door))
                door.ChangeAirtight = true;

            _console.RefreshShuttleConsoles();
        }

        private void OnRequestUndock(EntityUid uid, ShuttleConsoleComponent component, UndockRequestMessage args)
        {
            var dork = GetEntity(args.DockEntity);

            // TODO: Validation
            if (!TryComp<DockingComponent>(dork, out var dock) ||
                !dock.Docked ||
                HasComp<PreventPilotComponent>(Transform(uid).GridUid))
            {
                return;
            }

            Undock((dork, dock));
        }

        private void OnRequestDock(EntityUid uid, ShuttleConsoleComponent component, DockRequestMessage args)
        {
            if (HasComp<PreventPilotComponent>(Transform(uid).GridUid))
            {
                return;
            }

            if (!TryGetEntity(args.DockEntity, out var ourDock) ||
                !TryGetEntity(args.TargetDockEntity, out var targetDock) ||
                !TryComp(ourDock, out DockingComponent? ourDockComp) ||
                !TryComp(targetDock, out DockingComponent? targetDockComp))
            {
                _popup.PopupCursor(Loc.GetString("shuttle-console-dock-fail"));
                return;
            }

            // TODO: Move the CanDock stuff to the port state and also validate that stuff
            if (!CanDock())
            {
                _popup.PopupCursor(Loc.GetString("shuttle-console-dock-fail"));
                return;
            }

            // TODO: Make the API less ass
            // TODO: Add PreventPilot to CanDock
            // TODO: Validate it's our grid.
            Dock(ourDock.Value, ourDockComp, targetDock.Value, targetDockComp);
        }
    }
}
