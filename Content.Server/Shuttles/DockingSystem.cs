using Content.Server.Power.Components;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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
        [Dependency] private readonly SharedBroadphaseSystem _broadphaseSystem = default!;

        private const string DockingFixture = "docking";
        private const string DockingJoint = "docking";
        private const float DockingRadius = 0.3f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DockingComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DockingComponent, PowerChangedEvent>(OnPowerChange);
            SubscribeLocalEvent<DockingComponent, AnchorStateChangedEvent>(OnAnchorChange);

            SubscribeLocalEvent<DockingComponent, StartCollideEvent>(OnCollide);
            SubscribeLocalEvent<DockingComponent, EndCollideEvent>(OnEndCollide);
        }

        private void OnStartup(EntityUid uid, DockingComponent component, ComponentStartup args)
        {
            // Use startup so transform already initialized
            EnableDocking(uid, component);
        }

        // TODO: Probably better to have a funny UI for this but I hate UI so this is what you get.
        private void OnCollide(EntityUid uid, DockingComponent component, StartCollideEvent args)
        {
            if (component.DockedWith != null || args.OtherFixture.ID != DockingFixture) return;

            if (!EntityManager.TryGetComponent(uid, out DockingComponent? docking) ||
                !EntityManager.TryGetComponent(args.OtherFixture.Body.OwnerUid, out DockingComponent? otherDocking))
            {
                return;
            }

            // TODO: Should have a docking UI and make it optional
            Dock(docking, otherDocking);
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
            Logger.DebugS("docking", $"Docking between {dockA.Owner} and {dockB.Owner}");
            var weld = Get<SharedJointSystem>().CreateWeldJoint(dockA.OwnerUid, dockB.OwnerUid, DockingJoint);
        }

        private void Undock(DockingComponent dock)
        {
            DebugTools.Assert(dock.DockedWith != null);

            dock.DockedWith = null;

            //if (Get<SharedJointSystem>().Get)

            //Get<SharedJointSystem>().RemoveJoint();
            // TODO: Break weld
        }

        // So how docking works is that if 2 adjacent fixtures are colliding with 2 other docking fixtures the grids will "dock"
    }
}
