using Content.Server.Doors.Systems;
using Content.Server.Power.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Shuttles.Events;
using Content.Shared.Verbs;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems
{
    public sealed partial class DockingSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly FixtureSystem _fixtureSystem = default!;
        [Dependency] private readonly SharedJointSystem _jointSystem = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly ShuttleConsoleSystem _console = default!;
        [Dependency] private readonly DoorSystem _doorSystem = default!;

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
            SubscribeLocalEvent<DockingComponent, PowerChangedEvent>(OnPowerChange);
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

            if (!_mapManager.TryGetGrid(dockingXform.GridEntityId, out var grid) ||
                !HasComp<ShuttleComponent>(grid.GridEntityId)) return null;

            var transform = body.GetTransform();
            var dockingFixture = _fixtureSystem.GetFixtureOrNull(body, DockingFixture);

            // Happens if no power or whatever
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
            _mapManager.FindGridsIntersectingEnumerator(dockingXform.MapID, enlargedAABB, out var enumerator);

            while (enumerator.MoveNext(out var otherGrid))
            {
                if (otherGrid.GridEntityId == dockingXform.GridEntityId) continue;

                foreach (var ent in otherGrid.GetAnchoredEntities(enlargedAABB))
                {
                    if (!TryComp(ent, out DockingComponent? otherDocking) ||
                        !otherDocking.Enabled ||
                        !TryComp(ent, out PhysicsComponent? otherBody)) continue;

                    var otherTransform = otherBody.GetTransform();
                    var otherDockingFixture = _fixtureSystem.GetFixtureOrNull(otherBody, DockingFixture);

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

            dockA.DockJoint = null;
            dockA.DockedWith = null;

            // If these grids are ever invalid then need to look at fixing ordering for unanchored events elsewhere.
            var gridAUid = _mapManager.GetGrid(EntityManager.GetComponent<TransformComponent>(dockA.Owner).GridEntityId).GridEntityId;
            var gridBUid = _mapManager.GetGrid(EntityManager.GetComponent<TransformComponent>(dockB.Owner).GridEntityId).GridEntityId;

            var msg = new UndockEvent
            {
                DockA = dockA,
                DockB = dockB,
                GridAUid = gridAUid,
                GridBUid = gridBUid,
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

        private void OnPowerChange(EntityUid uid, DockingComponent component, PowerChangedEvent args)
        {
            var lifestage = MetaData(uid).EntityLifeStage;
            // This is because power can change during startup for <Reasons> and undock
            if (lifestage is < EntityLifeStage.MapInitialized or >= EntityLifeStage.Terminating) return;

            if (args.Powered)
            {
                EnableDocking(uid, component);
            }
            else
            {
                DisableDocking(uid, component);
            }
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

            _fixtureSystem.DestroyFixture(physicsComponent, DockingFixture);
        }

        private void EnableDocking(EntityUid uid, DockingComponent component)
        {
            if (component.Enabled)
                return;

            if (!TryComp(uid, out PhysicsComponent? physicsComponent))
                return;

            component.Enabled = true;

            // TODO: WTF IS THIS GARBAGE
            var shape = new PhysShapeCircle
            {
                // Want half of the unit vector
                Position = new Vector2(0f, -0.5f),
                Radius = DockingRadius
            };

            // Listen it makes intersection tests easier; you can probably dump this but it requires a bunch more boilerplate
            var fixture = new Fixture(physicsComponent, shape)
            {
                ID = DockingFixture,
                Hard = false,
            };

            // TODO: I want this to ideally be 2 fixtures to force them to have some level of alignment buuuttt
            // I also need collisionmanager for that yet again so they get dis.
            _fixtureSystem.TryCreateFixture(physicsComponent, fixture);
        }

        /// <summary>
        /// Docks 2 ports together and assumes it is valid.
        /// </summary>
        public void Dock(DockingComponent dockA, DockingComponent dockB)
        {
            _sawmill.Debug($"Docking between {dockA.Owner} and {dockB.Owner}");

            // https://gamedev.stackexchange.com/questions/98772/b2distancejoint-with-frequency-equal-to-0-vs-b2weldjoint

            // We could also potentially use a prismatic joint? Depending if we want clamps that can extend or whatever
            var dockAXform = EntityManager.GetComponent<TransformComponent>(dockA.Owner);
            var dockBXform = EntityManager.GetComponent<TransformComponent>(dockB.Owner);

            var gridA = _mapManager.GetGrid(dockAXform.GridEntityId).GridEntityId;
            var gridB = _mapManager.GetGrid(dockBXform.GridEntityId).GridEntityId;

            SharedJointSystem.LinearStiffness(
                2f,
                0.7f,
                EntityManager.GetComponent<PhysicsComponent>(gridA).Mass,
                EntityManager.GetComponent<PhysicsComponent>(gridB).Mass,
                out var stiffness,
                out var damping);

            // These need playing around with
            // Could also potentially have collideconnected false and stiffness 0 but it was a bit more suss???

            var joint = _jointSystem.GetOrCreateWeldJoint(gridA, gridB, DockingJoint + dockA.Owner);

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
            dockB.DockJoint = joint;

            if (TryComp(dockA.Owner, out DoorComponent? doorA))
            {
                doorA.ChangeAirtight = false;
                _doorSystem.StartOpening(doorA.Owner, doorA);
            }

            if (TryComp(dockB.Owner, out DoorComponent? doorB))
            {
                doorB.ChangeAirtight = false;
                _doorSystem.StartOpening(doorB.Owner, doorB);
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

            var fixtureA = _fixtureSystem.GetFixtureOrNull(bodyA, DockingFixture);
            var fixtureB = _fixtureSystem.GetFixtureOrNull(bodyB, DockingFixture);

            if (fixtureA == null || fixtureB == null)
            {
                return false;
            }

            var transformA = bodyA.GetTransform();
            var transformB = bodyB.GetTransform();
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

        private void Undock(DockingComponent dock)
        {
            if (dock.DockedWith == null)
            {
                DebugTools.Assert(false);
                _sawmill.Error($"Tried to undock {(dock).Owner} but not docked with anything?");
                return;
            }

            if (TryComp(dock.Owner, out DoorComponent? doorA))
            {
                doorA.ChangeAirtight = true;
                _doorSystem.TryClose(doorA.Owner, doorA);
            }

            if (TryComp(dock.DockedWith, out DoorComponent? doorB))
            {
                doorB.ChangeAirtight = true;
                _doorSystem.TryClose(doorB.Owner, doorB);
            }

            var recentlyDocked = EnsureComp<RecentlyDockedComponent>(dock.Owner);
            recentlyDocked.LastDocked = dock.DockedWith.Value;
            recentlyDocked = EnsureComp<RecentlyDockedComponent>(dock.DockedWith.Value);
            recentlyDocked.LastDocked = dock.DockedWith.Value;

            Cleanup(dock);
        }
    }
}
