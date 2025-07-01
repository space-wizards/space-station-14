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
        [Dependency] private readonly SharedMapSystem _mapSystem = default!;
        [Dependency] private readonly DoorSystem _doorSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly PathfindingSystem _pathfinding = default!;
        [Dependency] private readonly ShuttleConsoleSystem _console = default!;
        [Dependency] private readonly SharedJointSystem _jointSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        private const string DockingJoint = "docking";

        private EntityQuery<MapGridComponent> _gridQuery;
        private EntityQuery<PhysicsComponent> _physicsQuery;
        private EntityQuery<TransformComponent> _xformQuery;

        private readonly HashSet<Entity<DockingComponent>> _dockingSet = new();
        private readonly HashSet<Entity<DockingComponent, DoorBoltComponent>> _dockingBoltSet = new();

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

        public void UndockDocks(EntityUid gridUid)
        {
            _dockingSet.Clear();
            _lookup.GetChildEntities(gridUid, _dockingSet);

            foreach (var dock in _dockingSet)
            {
                Undock(dock);
            }
        }

        public void SetDockBolts(EntityUid gridUid, bool enabled)
        {
            _dockingBoltSet.Clear();
            _lookup.GetChildEntities(gridUid, _dockingBoltSet);

            foreach (var entity in _dockingBoltSet)
            {
                _doorSystem.TryClose(entity);
                _doorSystem.SetBoltsDown((entity.Owner, entity.Comp2), enabled);
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

            // This little gem is for docking deserialization
            if (component.DockedWith != null)
            {
                // They're still initialising so we'll just wait for both to be ready.
                if (MetaData(component.DockedWith.Value).EntityLifeStage < EntityLifeStage.Initialized)
                    return;

                var otherDock = EntityManager.GetComponent<DockingComponent>(component.DockedWith.Value);
                DebugTools.Assert(otherDock.DockedWith != null);

                Dock((uid, component), (component.DockedWith.Value, otherDock));
                DebugTools.Assert(component.Docked && otherDock.Docked);
            }
        }

        private void OnAnchorChange(Entity<DockingComponent> entity, ref AnchorStateChangedEvent args)
        {
            if (!args.Anchored)
            {
                Undock(entity);
            }
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
            Dock((uid, component), (otherDock.Value, other));
            _console.RefreshShuttleConsoles();
        }

        /// <summary>
        /// Docks 2 ports together and assumes it is valid.
        /// </summary>
        public void Dock(Entity<DockingComponent> dockA, Entity<DockingComponent> dockB)
        {
            var dockAUid = dockA.Owner;
            var dockBUid = dockB.Owner;

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
                if (dockA.Comp.DockJointId != null)
                {
                    DebugTools.Assert(dockB.Comp.DockJointId == dockA.Comp.DockJointId);
                    joint = _jointSystem.GetOrCreateWeldJoint(gridA, gridB, dockA.Comp.DockJointId);
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
                joint.ReferenceAngle = (float)(_transform.GetWorldRotation(gridBXform) - _transform.GetWorldRotation(gridAXform));
                joint.CollideConnected = true;
                joint.Stiffness = stiffness;
                joint.Damping = damping;

                dockA.Comp.DockJoint = joint;
                dockA.Comp.DockJointId = joint.ID;

                dockB.Comp.DockJoint = joint;
                dockB.Comp.DockJointId = joint.ID;
            }

            dockA.Comp.DockedWith = dockBUid;
            dockB.Comp.DockedWith = dockAUid;

            if (TryComp(dockAUid, out DoorComponent? doorA))
            {
                if (_doorSystem.TryOpen(dockAUid, doorA))
                {
                    if (TryComp<DoorBoltComponent>(dockAUid, out var airlockA))
                    {
                        _doorSystem.SetBoltsDown((dockAUid, airlockA), true);
                    }
                }
                doorA.ChangeAirtight = false;
            }

            if (TryComp(dockBUid, out DoorComponent? doorB))
            {
                if (_doorSystem.TryOpen(dockBUid, doorB))
                {
                    if (TryComp<DoorBoltComponent>(dockBUid, out var airlockB))
                    {
                        _doorSystem.SetBoltsDown((dockBUid, airlockB), true);
                    }
                }
                doorB.ChangeAirtight = false;
            }

            if (_pathfinding.TryCreatePortal(dockAXform.Coordinates, dockBXform.Coordinates, out var handle))
            {
                dockA.Comp.PathfindHandle = handle;
                dockB.Comp.PathfindHandle = handle;
            }

            var msg = new DockEvent
            {
                DockA = dockA,
                DockB = dockB,
                GridAUid = gridA,
                GridBUid = gridB,
            };

            _console.RefreshShuttleConsoles();
            RaiseLocalEvent(dockAUid, msg);
            RaiseLocalEvent(dockBUid, msg);
            RaiseLocalEvent(msg);
        }

        /// <summary>
        /// Attempts to dock 2 ports together and will return early if it's not possible.
        /// </summary>
        private void TryDock(Entity<DockingComponent> dockA, Entity<DockingComponent> dockB)
        {
            if (!CanDock(dockA, dockB))
                return;

            Dock(dockA, dockB);
        }

        public void Undock(Entity<DockingComponent> dock)
        {
            if (dock.Comp.DockedWith == null)
                return;

            OnUndock(dock.Owner);
            OnUndock(dock.Comp.DockedWith.Value);
            Cleanup(dock.Owner, dock);
            _console.RefreshShuttleConsoles();
        }

        private void OnUndock(EntityUid dockUid)
        {
            if (TerminatingOrDeleted(dockUid))
                return;

            if (TryComp<DoorBoltComponent>(dockUid, out var airlock))
                _doorSystem.SetBoltsDown((dockUid, airlock), false);

            if (TryComp(dockUid, out DoorComponent? door) && _doorSystem.TryClose(dockUid, door))
                door.ChangeAirtight = true;
        }

        private void OnRequestUndock(EntityUid uid, ShuttleConsoleComponent component, UndockRequestMessage args)
        {
            if (!TryGetEntity(args.DockEntity, out var dockEnt) ||
                !TryComp(dockEnt, out DockingComponent? dockComp))
            {
                _popup.PopupCursor(Loc.GetString("shuttle-console-undock-fail"));
                return;
            }

            var dock = (dockEnt.Value, dockComp);

            if (!CanUndock(dock))
            {
                _popup.PopupCursor(Loc.GetString("shuttle-console-undock-fail"));
                return;
            }

            Undock(dock);
        }

        private void OnRequestDock(EntityUid uid, ShuttleConsoleComponent component, DockRequestMessage args)
        {
            var console = _console.GetDroneConsole(uid);

            if (console == null)
            {
                _popup.PopupCursor(Loc.GetString("shuttle-console-dock-fail"));
                return;
            }

            var shuttleUid = Transform(console.Value).GridUid;

            if (!CanShuttleDock(shuttleUid))
            {
                _popup.PopupCursor(Loc.GetString("shuttle-console-dock-fail"));
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

            // Cheating?
            if (!TryComp(ourDock, out TransformComponent? xformA) ||
                xformA.GridUid != shuttleUid)
            {
                _popup.PopupCursor(Loc.GetString("shuttle-console-dock-fail"));
                return;
            }

            // TODO: Move the CanDock stuff to the port state and also validate that stuff
            // Also need to check preventpilot + enabled / dockedwith
            if (!CanDock((ourDock.Value, ourDockComp), (targetDock.Value, targetDockComp)))
            {
                _popup.PopupCursor(Loc.GetString("shuttle-console-dock-fail"));
                return;
            }

            Dock((ourDock.Value, ourDockComp), (targetDock.Value, targetDockComp));
        }

        public bool CanUndock(Entity<DockingComponent?> dock)
        {
            if (!Resolve(dock, ref dock.Comp) ||
                !dock.Comp.Docked)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if both docks can connect. Does not consider whether the shuttle allows it.
        /// </summary>
        public bool CanDock(Entity<DockingComponent> dockA, Entity<DockingComponent> dockB)
        {
            if (dockA.Comp.DockedWith != null ||
                dockB.Comp.DockedWith != null)
            {
                return false;
            }

            var xformA = Transform(dockA);
            var xformB = Transform(dockB);

            if (!xformA.Anchored || !xformB.Anchored)
                return false;

            var (worldPosA, worldRotA) = XformSystem.GetWorldPositionRotation(xformA);
            var (worldPosB, worldRotB) = XformSystem.GetWorldPositionRotation(xformB);

            return CanDock(new MapCoordinates(worldPosA, xformA.MapID), worldRotA,
                new MapCoordinates(worldPosB, xformB.MapID), worldRotB);
        }
    }
}
