using Content.Server.Doors.Systems;
using Content.Server.NPC.Pathfinding;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Shuttles.Events;
using Robust.Shared.Map;
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
        [Dependency] private readonly AirlockSystem _airlocks = default!;
        [Dependency] private readonly DoorSystem _doorSystem = default!;
        [Dependency] private readonly FixtureSystem _fixtureSystem = default!;
        [Dependency] private readonly PathfindingSystem _pathfinding = default!;
        [Dependency] private readonly ShuttleConsoleSystem _console = default!;
        [Dependency] private readonly SharedJointSystem _jointSystem = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

        private ISawmill _sawmill = default!;
        private const string DockingFixture = "docking";
        private const string DockingJoint = "docking";
        private const float DockingRadius = 0.20f;

        public override void Initialize()
        {
            base.Initialize();
            _sawmill = Logger.GetSawmill("docking");
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

        private DockingComponent? GetDockable(PhysicsComponent body, TransformComponent dockingXform)
        {
            // Did you know Saltern is the most dockable station?

            // Assume the docking port itself (and its body) is valid

            if (!_mapManager.TryGetGrid(dockingXform.GridUid, out var grid) ||
                !HasComp<ShuttleComponent>(grid.Owner))
            {
                return null;
            }

            var transform = _physics.GetPhysicsTransform(body.Owner, dockingXform);
            var dockingFixture = _fixtureSystem.GetFixtureOrNull(body.Owner, DockingFixture);

            if (dockingFixture == null)
                return null;

            Box2? aabb = null;

            for (var i = 0; i < dockingFixture.Shape.ChildCount; i++)
            {
                aabb = aabb?.Union(dockingFixture.Shape.ComputeAABB(transform, i)) ?? dockingFixture.Shape.ComputeAABB(transform, i);
            }

            if (aabb == null) return null;

            var enlargedAABB = aabb.Value.Enlarged(DockingRadius * 1.5f);

            // Get any docking ports in range on other grids.
            foreach (var otherGrid in _mapManager.FindGridsIntersecting(dockingXform.MapID, enlargedAABB))
            {
                if (otherGrid.Owner == dockingXform.GridUid)
                    continue;

                foreach (var ent in otherGrid.GetAnchoredEntities(enlargedAABB))
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
                        _sawmill.Error($"Found null docking fixture on {ent}");
                        continue;
                    }

                    for (var i = 0; i < otherDockingFixture.Shape.ChildCount; i++)
                    {
                        var otherAABB = otherDockingFixture.Shape.ComputeAABB(otherTransform, i);

                        if (!aabb.Value.Intersects(otherAABB)) continue;

                        // TODO: Need CollisionManager's GJK for accurate bounds
                        // Realistically I want 2 fixtures anyway but I'll deal with that later.
                        return otherDocking;
                    }
                }
            }

            return null;
        }

        private void OnShutdown(EntityUid uid, DockingComponent component, ComponentShutdown args)
        {
            if (component.DockedWith == null ||
                EntityManager.GetComponent<MetaDataComponent>(uid).EntityLifeStage > EntityLifeStage.MapInitialized) return;

            Cleanup(component);
        }

        private void Cleanup(DockingComponent dockA)
        {
            _pathfinding.RemovePortal(dockA.PathfindHandle);
            _jointSystem.RemoveJoint(dockA.DockJoint!);

            var dockBUid = dockA.DockedWith;

            if (dockBUid == null ||
                dockA.DockJoint == null ||
                !TryComp(dockBUid, out DockingComponent? dockB))
            {
                DebugTools.Assert(false);
                _sawmill.Error($"Tried to cleanup {dockA.Owner} but not docked?");

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
            var gridAUid = EntityManager.GetComponent<TransformComponent>(dockA.Owner).GridUid;
            var gridBUid = EntityManager.GetComponent<TransformComponent>(dockB.Owner).GridUid;
            DebugTools.Assert(gridAUid != null);
            DebugTools.Assert(gridBUid != null);

            var msg = new UndockEvent
            {
                DockA = dockA,
                DockB = dockB,
                GridAUid = gridAUid!.Value,
                GridBUid = gridBUid!.Value,
            };

            EntityManager.EventBus.RaiseLocalEvent(dockA.Owner, msg, false);
            EntityManager.EventBus.RaiseLocalEvent(dockB.Owner, msg, false);
            EntityManager.EventBus.RaiseEvent(EventSource.Local, msg);
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
                if (MetaData(component.DockedWith.Value).EntityLifeStage < EntityLifeStage.Initialized) return;

                var otherDock = EntityManager.GetComponent<DockingComponent>(component.DockedWith.Value);
                DebugTools.Assert(otherDock.DockedWith != null);

                Dock(component, otherDock);
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
            if (!component.Docked) return;

            var other = Comp<DockingComponent>(component.DockedWith!.Value);

            Undock(component);
            Dock(component, other);
            _console.RefreshShuttleConsoles();
        }

        private void DisableDocking(EntityUid uid, DockingComponent component)
        {
            if (!component.Enabled) return;

            component.Enabled = false;

            if (component.DockedWith != null)
            {
                Undock(component);
            }

            if (!TryComp(uid, out PhysicsComponent? physicsComponent))
            {
                return;
            }

            _fixtureSystem.DestroyFixture(uid, DockingFixture, body: physicsComponent);
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
            _fixtureSystem.TryCreateFixture(uid, shape, DockingFixture, hard: false, body: physicsComponent);
        }

        /// <summary>
        /// Docks 2 ports together and assumes it is valid.
        /// </summary>
        public void Dock(DockingComponent dockA, DockingComponent dockB)
        {
            if (dockB.Owner.GetHashCode() < dockA.Owner.GetHashCode())
            {
                (dockA, dockB) = (dockB, dockA);
            }

            _sawmill.Debug($"Docking between {dockA.Owner} and {dockB.Owner}");

            // https://gamedev.stackexchange.com/questions/98772/b2distancejoint-with-frequency-equal-to-0-vs-b2weldjoint

            // We could also potentially use a prismatic joint? Depending if we want clamps that can extend or whatever
            var dockAXform = EntityManager.GetComponent<TransformComponent>(dockA.Owner);
            var dockBXform = EntityManager.GetComponent<TransformComponent>(dockB.Owner);

            DebugTools.Assert(dockAXform.GridUid != null);
            DebugTools.Assert(dockBXform.GridUid != null);
            var gridA = dockAXform.GridUid!.Value;
            var gridB = dockBXform.GridUid!.Value;

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
                joint = _jointSystem.GetOrCreateWeldJoint(gridA, gridB, DockingJoint + dockA.Owner);
            }

            var gridAXform = EntityManager.GetComponent<TransformComponent>(gridA);
            var gridBXform = EntityManager.GetComponent<TransformComponent>(gridB);

            var anchorA = dockAXform.LocalPosition + dockAXform.LocalRotation.ToWorldVec() / 2f;
            var anchorB = dockBXform.LocalPosition + dockBXform.LocalRotation.ToWorldVec() / 2f;

            joint.LocalAnchorA = anchorA;
            joint.LocalAnchorB = anchorB;
            joint.ReferenceAngle = (float) (gridBXform.WorldRotation - gridAXform.WorldRotation);
            joint.CollideConnected = true;
            joint.Stiffness = stiffness;
            joint.Damping = damping;

            dockA.DockedWith = dockB.Owner;
            dockB.DockedWith = dockA.Owner;

            dockA.DockJoint = joint;
            dockA.DockJointId = joint.ID;

            dockB.DockJoint = joint;
            dockB.DockJointId = joint.ID;

            if (TryComp(dockA.Owner, out DoorComponent? doorA))
            {
                if (_doorSystem.TryOpen(doorA.Owner, doorA))
                {
                    doorA.ChangeAirtight = false;
                    if (TryComp<AirlockComponent>(dockA.Owner, out var airlockA))
                    {
                        _airlocks.SetBoltsWithAudio(dockA.Owner, airlockA, true);
                    }
                }
            }

            if (TryComp(dockB.Owner, out DoorComponent? doorB))
            {
                if (_doorSystem.TryOpen(doorB.Owner, doorB))
                {
                    doorB.ChangeAirtight = false;
                    if (TryComp<AirlockComponent>(dockB.Owner, out var airlockB))
                    {
                        _airlocks.SetBoltsWithAudio(dockB.Owner, airlockB, true);
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

            EntityManager.EventBus.RaiseLocalEvent(dockA.Owner, msg, false);
            EntityManager.EventBus.RaiseLocalEvent(dockB.Owner, msg, false);
            EntityManager.EventBus.RaiseEvent(EventSource.Local, msg);
        }

        private bool CanDock(DockingComponent dockA, DockingComponent dockB)
        {
            if (!TryComp(dockA.Owner, out PhysicsComponent? bodyA) ||
                !TryComp(dockB.Owner, out PhysicsComponent? bodyB) ||
                !dockA.Enabled ||
                !dockB.Enabled ||
                dockA.DockedWith != null ||
                dockB.DockedWith != null)
            {
                return false;
            }

            var fixtureA = _fixtureSystem.GetFixtureOrNull(bodyA.Owner, DockingFixture);
            var fixtureB = _fixtureSystem.GetFixtureOrNull(bodyB.Owner, DockingFixture);

            if (fixtureA == null || fixtureB == null)
            {
                return false;
            }

            var transformA = _physics.GetPhysicsTransform(dockA.Owner);
            var transformB = _physics.GetPhysicsTransform(dockB.Owner);
            var intersect = false;

            for (var i = 0; i < fixtureA.Shape.ChildCount; i++)
            {
                var aabb = fixtureA.Shape.ComputeAABB(transformA, i);

                for (var j = 0; j < fixtureB.Shape.ChildCount; j++)
                {
                    var otherAABB = fixtureB.Shape.ComputeAABB(transformB, j);
                    if (!aabb.Intersects(otherAABB)) continue;

                    // TODO: Need collisionmanager's GJK for accurate checks don't @ me son
                    intersect = true;
                    break;
                }

                if (intersect) break;
            }

            return intersect;
        }

        /// <summary>
        /// Attempts to dock 2 ports together and will return early if it's not possible.
        /// </summary>
        private void TryDock(DockingComponent dockA, DockingComponent dockB)
        {
            if (!CanDock(dockA, dockB)) return;

            Dock(dockA, dockB);
        }

        public void Undock(DockingComponent dock)
        {
            if (dock.DockedWith == null)
                return;

            if (TryComp<AirlockComponent>(dock.Owner, out var airlockA))
            {
                _airlocks.SetBoltsWithAudio(dock.Owner, airlockA, false);
            }

            if (TryComp<AirlockComponent>(dock.DockedWith, out var airlockB))
            {
                _airlocks.SetBoltsWithAudio(dock.DockedWith.Value, airlockB, false);
            }

            if (TryComp(dock.Owner, out DoorComponent? doorA))
            {
                if (_doorSystem.TryClose(doorA.Owner, doorA))
                {
                    doorA.ChangeAirtight = true;
                }
            }

            if (TryComp(dock.DockedWith, out DoorComponent? doorB))
            {
                if (_doorSystem.TryClose(doorB.Owner, doorB))
                {
                    doorB.ChangeAirtight = true;
                }
            }

            if (LifeStage(dock.Owner) < EntityLifeStage.Terminating)
            {
                var recentlyDocked = EnsureComp<RecentlyDockedComponent>(dock.Owner);
                recentlyDocked.LastDocked = dock.DockedWith.Value;
            }

            if (TryComp(dock.DockedWith.Value, out MetaDataComponent? meta) && meta.EntityLifeStage < EntityLifeStage.Terminating)
            {
                var recentlyDocked = EnsureComp<RecentlyDockedComponent>(dock.DockedWith.Value);
                recentlyDocked.LastDocked = dock.DockedWith.Value;
            }

            Cleanup(dock);
        }
    }
}
