using Content.Server.Doors.Components;
using Content.Server.Power.Components;
using Content.Server.Shuttles.Components;
using Content.Shared.Doors;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.EntitySystems
{
    public sealed class DockingSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly FixtureSystem _fixtureSystem = default!;
        [Dependency] private readonly SharedJointSystem _jointSystem = default!;

        private const string DockingFixture = "docking";
        private const string DockingJoint = "docking";
        private const float DockingRadius = 0.20f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DockingComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DockingComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<DockingComponent, PowerChangedEvent>(OnPowerChange);
            SubscribeLocalEvent<DockingComponent, AnchorStateChangedEvent>(OnAnchorChange);

            SubscribeLocalEvent<DockingComponent, GetInteractionVerbsEvent>(OnVerb);
            SubscribeLocalEvent<DockingComponent, BeforeDoorAutoCloseEvent>(OnAutoClose);
        }

        private void OnAutoClose(EntityUid uid, DockingComponent component, BeforeDoorAutoCloseEvent args)
        {
            // We'll just pin the door open when docked.
            if (component.Docked)
                args.Cancel();
        }

        private void OnVerb(EntityUid uid, DockingComponent component, GetInteractionVerbsEvent args)
        {
            if (!args.CanInteract ||
                !args.CanAccess) return;

            Verb? verb;

            // TODO: Have it open the UI and have the UI do this.
            if (!component.Docked &&
                EntityManager.TryGetComponent(uid, out PhysicsComponent? body) &&
                EntityManager.TryGetComponent(uid, out TransformComponent? xform))
            {
                DockingComponent? otherDock = null;

                if (component.Enabled)
                    otherDock = GetDockable(body, xform);

                verb = new Verb
                {
                    Disabled = otherDock == null,
                    Text = Loc.GetString("docking-component-dock"),
                    Act = () =>
                    {
                        if (otherDock == null) return;
                        TryDock(component, otherDock);
                    }
                };
            }
            else if (component.Docked)
            {
                verb = new Verb
                {
                    Disabled = !component.Docked,
                    Text = Loc.GetString("docking-component-undock"),
                    Act = () =>
                    {
                        if (component.DockedWith == null || !component.Enabled) return;

                        Undock(component);
                    }
                };
            }
            else
            {
                return;
            }

            args.Verbs.Add(verb);
        }

        private DockingComponent? GetDockable(PhysicsComponent body, TransformComponent dockingXform)
        {
            // Did you know Saltern is the most dockable station?

            // Assume the docking port itself (and its body) is valid

            if (!_mapManager.TryGetGrid(dockingXform.GridID, out var grid) ||
                !EntityManager.HasComponent<ShuttleComponent>(grid.GridEntityId)) return null;

            var transform = body.GetTransform();
            var dockingFixture = _fixtureSystem.GetFixtureOrNull(body, DockingFixture);

            if (dockingFixture == null)
            {
                DebugTools.Assert(false);
                Logger.ErrorS("docking", $"Found null fixture on {(body).Owner}");
                return null;
            }

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
                if (otherGrid.Index == dockingXform.GridID) continue;

                foreach (var ent in otherGrid.GetAnchoredEntities(enlargedAABB))
                {
                    if (!EntityManager.TryGetComponent(ent, out DockingComponent? otherDocking) ||
                        !otherDocking.Enabled ||
                        !EntityManager.TryGetComponent(ent, out PhysicsComponent? otherBody)) continue;

                    var otherTransform = otherBody.GetTransform();
                    var otherDockingFixture = _fixtureSystem.GetFixtureOrNull(otherBody, DockingFixture);

                    if (otherDockingFixture == null)
                    {
                        DebugTools.Assert(false);
                        Logger.ErrorS("docking", $"Found null docking fixture on {ent}");
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

            var dockB = dockA.DockedWith;

            if (dockB == null || dockA.DockJoint == null)
            {
                DebugTools.Assert(false);
                Logger.Error("docking", $"Tried to cleanup {(dockA).Owner} but not docked?");

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
            var gridAUid = _mapManager.GetGrid(EntityManager.GetComponent<TransformComponent>((dockA).Owner).GridID).GridEntityId;
            var gridBUid = _mapManager.GetGrid(EntityManager.GetComponent<TransformComponent>((dockB).Owner).GridID).GridEntityId;

            var msg = new UndockEvent
            {
                DockA = dockA,
                DockB = dockB,
                GridAUid = gridAUid,
                GridBUid = gridBUid,
            };

            EntityManager.EventBus.RaiseLocalEvent((dockA).Owner, msg, false);
            EntityManager.EventBus.RaiseLocalEvent((dockB).Owner, msg, false);
            EntityManager.EventBus.RaiseEvent(EventSource.Local, msg);
        }

        private void OnStartup(EntityUid uid, DockingComponent component, ComponentStartup args)
        {
            // Use startup so transform already initialized
            if (!EntityManager.GetComponent<TransformComponent>(uid).Anchored) return;

            EnableDocking(uid, component);
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
        }

        private void OnPowerChange(EntityUid uid, DockingComponent component, PowerChangedEvent args)
        {
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

            if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? physicsComponent))
            {
                return;
            }

            _fixtureSystem.DestroyFixture(physicsComponent, DockingFixture);
        }

        private void EnableDocking(EntityUid uid, DockingComponent component)
        {
            if (component.Enabled)
                return;

            if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? physicsComponent))
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
            _fixtureSystem.CreateFixture(physicsComponent, fixture);
        }

        /// <summary>
        /// Docks 2 ports together and assumes it is valid.
        /// </summary>
        private void Dock(DockingComponent dockA, DockingComponent dockB)
        {
            Logger.DebugS("docking", $"Docking between {dockA.Owner} and {dockB.Owner}");

            // https://gamedev.stackexchange.com/questions/98772/b2distancejoint-with-frequency-equal-to-0-vs-b2weldjoint

            // We could also potentially use a prismatic joint? Depending if we want clamps that can extend or whatever

            var dockAXform = EntityManager.GetComponent<TransformComponent>((dockA).Owner);
            var dockBXform = EntityManager.GetComponent<TransformComponent>((dockB).Owner);

            var gridA = _mapManager.GetGrid(dockAXform.GridID).GridEntityId;
            var gridB = _mapManager.GetGrid(dockBXform.GridID).GridEntityId;

            SharedJointSystem.LinearStiffness(
                2f,
                0.7f,
                EntityManager.GetComponent<PhysicsComponent>(gridA).Mass,
                EntityManager.GetComponent<PhysicsComponent>(gridB).Mass,
                out var stiffness,
                out var damping);

            // These need playing around with
            // Could also potentially have collideconnected false and stiffness 0 but it was a bit more suss???
            var joint = _jointSystem.CreateWeldJoint(gridA, gridB, DockingJoint + (dockA).Owner);

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

            dockA.DockedWith = dockB;
            dockB.DockedWith = dockA;
            dockA.DockJoint = joint;
            dockB.DockJoint = joint;

            if (EntityManager.TryGetComponent((dockA).Owner, out ServerDoorComponent? doorA))
            {
                doorA.ChangeAirtight = false;
                doorA.Open();
            }

            if (EntityManager.TryGetComponent((dockB).Owner, out ServerDoorComponent? doorB))
            {
                doorB.ChangeAirtight = false;
                doorB.Open();
            }

            var msg = new DockEvent
            {
                DockA = dockA,
                DockB = dockB,
                GridAUid = gridA,
                GridBUid = gridB,
            };

            EntityManager.EventBus.RaiseLocalEvent((dockA).Owner, msg, false);
            EntityManager.EventBus.RaiseLocalEvent((dockB).Owner, msg, false);
            EntityManager.EventBus.RaiseEvent(EventSource.Local, msg);
        }

        /// <summary>
        /// Attempts to dock 2 ports together and will return early if it's not possible.
        /// </summary>
        private void TryDock(DockingComponent dockA, DockingComponent dockB)
        {
            if (!EntityManager.TryGetComponent((dockA).Owner, out PhysicsComponent? bodyA) ||
                !EntityManager.TryGetComponent((dockB).Owner, out PhysicsComponent? bodyB) ||
                !dockA.Enabled ||
                !dockB.Enabled)
            {
                return;
            }

            var fixtureA = _fixtureSystem.GetFixtureOrNull(bodyA, DockingFixture);
            var fixtureB = _fixtureSystem.GetFixtureOrNull(bodyB, DockingFixture);

            if (fixtureA == null || fixtureB == null)
            {
                return;
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

            if (!intersect) return;

            Dock(dockA, dockB);
        }

        private void Undock(DockingComponent dock)
        {
            if (dock.DockedWith == null)
            {
                DebugTools.Assert(false);
                Logger.ErrorS("docking", $"Tried to undock {(dock).Owner} but not docked with anything?");
                return;
            }

            if (EntityManager.TryGetComponent((dock).Owner, out ServerDoorComponent? doorA))
            {
                doorA.ChangeAirtight = true;
                doorA.Close();
            }

            if (EntityManager.TryGetComponent((dock.DockedWith).Owner, out ServerDoorComponent? doorB))
            {
                doorB.ChangeAirtight = true;
                doorB.Close();
            }

            // Could maybe give the shuttle a light push away, or at least if there's no other docks left?

            Cleanup(dock);
        }

        /// <summary>
        /// Raised whenever 2 airlocks dock.
        /// </summary>
        public sealed class DockEvent : EntityEventArgs
        {
            public DockingComponent DockA = default!;
            public DockingComponent DockB = default!;

            public EntityUid GridAUid = default!;
            public EntityUid GridBUid = default!;
        }

        /// <summary>
        /// Raised whenever 2 grids undock.
        /// </summary>
        public sealed class UndockEvent : EntityEventArgs
        {
            public DockingComponent DockA = default!;
            public DockingComponent DockB = default!;

            public EntityUid GridAUid = default!;
            public EntityUid GridBUid = default!;
        }
    }
}
