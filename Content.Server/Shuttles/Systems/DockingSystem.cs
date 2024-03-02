using System.Numerics;
using Content.Server.Doors.Systems;
using Content.Server.NPC.Pathfinding;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Shuttles.Events;
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
    public sealed partial class DockingSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly DoorSystem _doorSystem = default!;
        [Dependency] private readonly FixtureSystem _fixtureSystem = default!;
        [Dependency] private readonly PathfindingSystem _pathfinding = default!;
        [Dependency] private readonly ShuttleConsoleSystem _console = default!;
        [Dependency] private readonly SharedJointSystem _jointSystem = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        private const string DockingFixture = "docking";
        private const string DockingJoint = "docking";
        private const float DockingRadius = 0.20f;

        private EntityQuery<PhysicsComponent> _physicsQuery;

        public override void Initialize()
        {
            base.Initialize();
            _physicsQuery = GetEntityQuery<PhysicsComponent>();

            SubscribeLocalEvent<DockingComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DockingComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<DockingComponent, AnchorStateChangedEvent>(OnAnchorChange);
            SubscribeLocalEvent<DockingComponent, ReAnchorEvent>(OnDockingReAnchor);

            SubscribeLocalEvent<DockingComponent, BeforeDoorAutoCloseEvent>(OnAutoClose);

            // Yes this isn't in shuttle console; it may be used by other systems technically.
            // in which case I would also add their subs here.
            SubscribeLocalEvent<ShuttleConsoleComponent, AutodockRequestMessage>(OnRequestAutodock);
            SubscribeLocalEvent<ShuttleConsoleComponent, StopAutodockRequestMessage>(OnRequestStopAutodock);
            SubscribeLocalEvent<ShuttleConsoleComponent, UndockRequestMessage>(OnRequestUndock);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            UpdateAutodock();
        }

        private void OnAutoClose(EntityUid uid, DockingComponent component, BeforeDoorAutoCloseEvent args)
        {
            // We'll just pin the door open when docked.
            if (component.Docked)
                args.Cancel();
        }

        private Entity<DockingComponent>? GetDockable(EntityUid uid, TransformComponent dockingXform)
        {
            // Did you know Saltern is the most dockable station?

            // Assume the docking port itself (and its body) is valid

            if (!HasComp<ShuttleComponent>(dockingXform.GridUid))
            {
                return null;
            }

            var transform = _physics.GetPhysicsTransform(uid, dockingXform);
            var dockingFixture = _fixtureSystem.GetFixtureOrNull(uid, DockingFixture);

            if (dockingFixture == null)
                return null;

            Box2? aabb = null;

            for (var i = 0; i < dockingFixture.Shape.ChildCount; i++)
            {
                aabb = aabb?.Union(dockingFixture.Shape.ComputeAABB(transform, i)) ?? dockingFixture.Shape.ComputeAABB(transform, i);
            }

            if (aabb == null)
                return null;

            var enlargedAABB = aabb.Value.Enlarged(DockingRadius * 1.5f);

            // Get any docking ports in range on other grids.
            var grids = new List<Entity<MapGridComponent>>();
            _mapManager.FindGridsIntersecting(dockingXform.MapID, enlargedAABB, ref grids);
            foreach (var otherGrid in grids)
            {
                if (otherGrid.Owner == dockingXform.GridUid)
                    continue;

                foreach (var ent in otherGrid.Comp.GetAnchoredEntities(enlargedAABB))
                {
                    if (!TryComp(ent, out DockingComponent? otherDocking) ||
                        !otherDocking.Enabled ||
                        !TryComp(ent, out FixturesComponent? otherBody))
                    {
                        continue;
                    }

                    var otherTransform = _physics.GetPhysicsTransform(ent);
                    var otherDockingFixture = _fixtureSystem.GetFixtureOrNull(ent, DockingFixture, manager: otherBody);

                    if (otherDockingFixture == null)
                    {
                        DebugTools.Assert(false);
                        Log.Error($"Found null docking fixture on {ent}");
                        continue;
                    }

                    for (var i = 0; i < otherDockingFixture.Shape.ChildCount; i++)
                    {
                        var otherAABB = otherDockingFixture.Shape.ComputeAABB(otherTransform, i);

                        if (!aabb.Value.Intersects(otherAABB))
                            continue;

                        // TODO: Need CollisionManager's GJK for accurate bounds
                        // Realistically I want 2 fixtures anyway but I'll deal with that later.
                        return (ent, otherDocking);
                    }
                }
            }

            return null;
        }

        private void OnShutdown(EntityUid uid, DockingComponent component, ComponentShutdown args)
        {
            if (component.DockedWith == null ||
                EntityManager.GetComponent<MetaDataComponent>(uid).EntityLifeStage > EntityLifeStage.MapInitialized)
            {
                return;
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

        private void OnStartup(EntityUid uid, DockingComponent component, ComponentStartup args)
        {
            // Use startup so transform already initialized
            if (!EntityManager.GetComponent<TransformComponent>(uid).Anchored) return;

            EnableDocking(uid, component);

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

        private void OnAnchorChange(EntityUid uid, DockingComponent component, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
            {
                EnableDocking(uid, component);
            }
            else
            {
                DisableDocking(uid, component);
            }

            _console.RefreshShuttleConsoles();
        }

        private void OnDockingReAnchor(EntityUid uid, DockingComponent component, ref ReAnchorEvent args)
        {
            if (!component.Docked)
                return;

            var otherDock = component.DockedWith;
            var other = Comp<DockingComponent>(otherDock!.Value);

            Undock(uid, component);
            Dock(uid, component, otherDock.Value, other);
            _console.RefreshShuttleConsoles();
        }

        private void DisableDocking(EntityUid uid, DockingComponent component)
        {
            if (!component.Enabled)
                return;

            component.Enabled = false;

            if (component.DockedWith != null)
            {
                Undock(uid, component);
            }
        }

        private void EnableDocking(EntityUid uid, DockingComponent component)
        {
            if (component.Enabled)
                return;

            if (!TryComp(uid, out PhysicsComponent? physicsComponent))
                return;

            component.Enabled = true;

            var shape = new PhysShapeCircle(DockingRadius, new Vector2(0f, -0.5f));

            // Listen it makes intersection tests easier; you can probably dump this but it requires a bunch more boilerplate
            // TODO: I want this to ideally be 2 fixtures to force them to have some level of alignment buuuttt
            // I also need collisionmanager for that yet again so they get dis.
            // TODO: CollisionManager is fine so get to work sloth chop chop.
            _fixtureSystem.TryCreateFixture(uid, shape, DockingFixture, hard: false, body: physicsComponent);
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
                        _doorSystem.SetBoltsDown((dockAUid, airlockA), true);
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
                        _doorSystem.SetBoltsDown((dockBUid, airlockB), true);
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

            var fixtureA = _fixtureSystem.GetFixtureOrNull(dockAUid, DockingFixture);
            var fixtureB = _fixtureSystem.GetFixtureOrNull(dockBUid, DockingFixture);

            if (fixtureA == null || fixtureB == null)
            {
                return false;
            }

            var transformA = _physics.GetPhysicsTransform(dockAUid);
            var transformB = _physics.GetPhysicsTransform(dockBUid);
            var intersect = false;

            for (var i = 0; i < fixtureA.Shape.ChildCount; i++)
            {
                var aabb = fixtureA.Shape.ComputeAABB(transformA, i);

                for (var j = 0; j < fixtureB.Shape.ChildCount; j++)
                {
                    var otherAABB = fixtureB.Shape.ComputeAABB(transformB, j);
                    if (!aabb.Intersects(otherAABB))
                        continue;

                    // TODO: Need collisionmanager's GJK for accurate checks don't @ me son
                    intersect = true;
                    break;
                }

                if (intersect)
                    break;
            }

            return intersect;
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

        public void Undock(EntityUid dockUid, DockingComponent dock)
        {
            if (dock.DockedWith == null)
                return;

            OnUndock(dockUid, dock.DockedWith.Value);
            OnUndock(dock.DockedWith.Value, dockUid);
            Cleanup(dockUid, dock);
        }

        private void OnUndock(EntityUid dockUid, EntityUid other)
        {
            if (TerminatingOrDeleted(dockUid))
                return;

            if (TryComp<DoorBoltComponent>(dockUid, out var airlock))
                _doorSystem.SetBoltsDown((dockUid, airlock), false);

            if (TryComp(dockUid, out DoorComponent? door) && _doorSystem.TryClose(dockUid, door))
                door.ChangeAirtight = true;

            var recentlyDocked = EnsureComp<RecentlyDockedComponent>(dockUid);
            recentlyDocked.LastDocked = other;
        }
    }
}
