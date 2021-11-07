using System.Collections.Generic;
using Content.Server.Power.Components;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Shuttles
{
    [RegisterComponent]
    public sealed class DockingComponent : Component
    {
        public override string Name => "Docking";

        [ViewVariables]
        public bool Enabled = false;

        [ViewVariables]
        public DockingComponent? DockedWith;
    }

    public class DockingSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly SharedBroadphaseSystem _broadphaseSystem = default!;
        [Dependency] private readonly SharedJointSystem _jointSystem = default!;

        private const string DockingFixture = "docking";
        private const string DockingJoint = "docking";
        private const float DockingRadius = 0.3f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DockingComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DockingComponent, PowerChangedEvent>(OnPowerChange);
            SubscribeLocalEvent<DockingComponent, AnchorStateChangedEvent>(OnAnchorChange);

            // Forgive me
            SubscribeLocalEvent<ShuttleComponent, MoveEvent>(OnShuttleMove);
            SubscribeLocalEvent<ShuttleComponent, RotateEvent>(OnShuttleRotate);
        }

        private void OnShuttleRotate(EntityUid uid, ShuttleComponent component, ref RotateEvent args)
        {
            CheckDocking(uid, component);
        }

        private void OnShuttleMove(EntityUid uid, ShuttleComponent component, ref MoveEvent args)
        {
            CheckDocking(uid, component);
        }

        private void CheckDocking(EntityUid uid, ShuttleComponent component)
        {
            // Having bodies hanging off the edge of a grid colliding with another grid turns out to be
            // hard as balls hence you get this for now. Don't @ me

            // TODO: So ideally we would have a local UI for the docking port so the client can just check this every tick
            // and then send to the server "hey please dock" which checks it once.
            // Given there's only gonna be like... 10 docking ports for a while this isn't the worst code.
            var shuttleXform = EntityManager.GetComponent<ITransformComponent>(uid);
            var dockingPorts = new List<(DockingComponent, PhysicsComponent, ITransformComponent)>();

            foreach (var (docking, body, xform) in EntityManager.EntityQuery<DockingComponent, PhysicsComponent, ITransformComponent>())
            {
                if (xform.MapID != shuttleXform.MapID || !docking.Enabled || docking.DockedWith != null || body.GetFixture(DockingFixture) == null) continue;

                dockingPorts.Add((docking, body, xform));
            }

            // Check for any docking collisions
            foreach (var (docking, body, xform) in dockingPorts)
            {
                if (xform.GridID != shuttleXform.GridID || docking.DockedWith != null) continue;

                var transform = body.GetTransform();
                var dockingFixture = body.GetFixture(DockingFixture)!;
                var stop = false;

                // Check for any docking collisions
                foreach (var (otherDocking, otherBody, otherXform) in dockingPorts)
                {
                    if (otherXform.GridID == xform.GridID ||
                        otherDocking.DockedWith != null) continue;

                    if (stop) break;

                    var otherTransform = otherBody.GetTransform();
                    var otherDockingFixture = otherBody.GetFixture(DockingFixture);

                    for (var i = 0; i < dockingFixture.Shape.ChildCount; i++)
                    {
                        if (stop) break;

                        var aabb = dockingFixture.Shape.ComputeAABB(transform, i);

                        for (var j = 0; j < dockingFixture.Shape.ChildCount; j++)
                        {
                            var otherAABB = otherDockingFixture!.Shape.ComputeAABB(otherTransform, j);

                            if (!aabb.Intersects(otherAABB)) continue;

                            stop = true;
                            Dock(docking, otherDocking);
                            break;
                        }
                    }
                }
            }
        }

        private void OnStartup(EntityUid uid, DockingComponent component, ComponentStartup args)
        {
            // Use startup so transform already initialized
            EnableDocking(uid, component);
        }

        private void OnEndCollide(EntityUid uid, DockingComponent component, EndCollideEvent args)
        {
            /*
            if (component.DockedWith != null) return;

            if (!EntityManager.TryGetComponent(uid, out DockingComponent? otherDocking))
            {
                return;
            }

            component.CollidingCount -= 1;
            // TODO: Decrease if ending with another docking comp

            throw new System.NotImplementedException();
            */
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

            // TODO: Break
            _broadphaseSystem.DestroyFixture(physicsComponent, DockingFixture);
        }

        private void EnableDocking(EntityUid uid, DockingComponent component)
        {
            if (component.Enabled) return;

            if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? physicsComponent))
            {
                return;
            }

            component.Enabled = true;

            var xform = EntityManager.GetComponent<ITransformComponent>(uid);

            var shape = new PhysShapeCircle
            {
                // Want half of the unit vector
                Position = xform.LocalRotation.ToWorldVec() / 2f,
                Radius = DockingRadius
            };

            // TODO: Its own collision mask / layer probably
            var fixture = new Fixture(physicsComponent, shape)
            {
                ID = DockingFixture,
                Hard = false,
                CollisionMask = (int) CollisionGroup.Docking,
                CollisionLayer = (int) CollisionGroup.Docking,
            };

            _broadphaseSystem.CreateFixture(physicsComponent, fixture);
        }

        private void Dock(DockingComponent dockA, DockingComponent dockB)
        {
            var gridA = _mapManager.GetGrid(dockA.Owner.Transform.GridID).GridEntityId;
            var gridB = _mapManager.GetGrid(dockB.Owner.Transform.GridID).GridEntityId;

            var dockAXform = EntityManager.GetComponent<ITransformComponent>(dockA.OwnerUid);
            var dockBXform = EntityManager.GetComponent<ITransformComponent>(dockB.OwnerUid);

            Logger.DebugS("docking", $"Docking between {dockA.Owner} and {dockB.Owner}");

            var weld = _jointSystem.CreateWeldJoint(gridA, gridB, DockingJoint);

            weld.LocalAnchorA = dockBXform.LocalPosition + dockBXform.LocalRotation.ToWorldVec() / 2f;
            weld.LocalAnchorB = dockAXform.LocalPosition + dockAXform.LocalRotation.ToWorldVec() / 2f;
            weld.ReferenceAngle = 0f;
            weld.CollideConnected = false;

            dockA.DockedWith = dockB;
            dockB.DockedWith = dockA;
            // TODO: Try to dock with all other ports
        }

        private void Undock(DockingComponent dock)
        {
            DebugTools.Assert(dock.DockedWith != null);

            dock.DockedWith = null;

            //Get<SharedJointSystem>().RemoveJoint();
            // TODO: Break weld
        }

        private void Undock(ShuttleComponent shuttleA, ShuttleComponent shuttleB)
        {
            var shuttleAXform = EntityManager.GetComponent<ITransformComponent>(shuttleA.OwnerUid);
            var shuttleBXform = EntityManager.GetComponent<ITransformComponent>(shuttleB.OwnerUid);

            foreach (var (docking, body, xform) in EntityManager
                .EntityQuery<DockingComponent, PhysicsComponent, ITransformComponent>())
            {
                // We'll always just check ShuttleA's docks and assume all of ShuttleB's will be cleaned up as a result.
                if (shuttleAXform.MapID != xform.MapID ||
                    docking.DockedWith == null ||
                    xform.GridID != shuttleAXform.GridID) continue;

                Undock(docking);
            }
        }
    }
}
