using Content.Server.Power.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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
        public bool Enabled = true;

        public int CollidingCount = 0;

        [ViewVariables]
        public DockingComponent? DockedWith;
    }

    public class DockingSystem : EntitySystem
    {
        [Dependency] private readonly SharedBroadphaseSystem _broadphaseSystem = default!;

        private const string DockingFixture = "docking";
        private const float DockingRadius = 1f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DockingComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<DockingComponent, PowerChangedEvent>(OnPowerChange);
            SubscribeLocalEvent<DockingComponent, AnchorStateChangedEvent>(OnAnchorChange);

            SubscribeLocalEvent<DockingComponent, StartCollideEvent>(OnCollide);
            SubscribeLocalEvent<DockingComponent, EndCollideEvent>(OnEndCollide);
        }

        private void OnInit(EntityUid uid, DockingComponent component, ComponentInit args)
        {
            EnableDocking(uid, component);
        }

        // TODO: Probably better to have a funny UI for this but I hate UI so this is what you get.
        private void OnCollide(EntityUid uid, DockingComponent component, StartCollideEvent args)
        {
            if (component.DockedWith != null) return;

            if (!EntityManager.TryGetComponent(uid, out DockingComponent? otherDocking))
            {
                return;
            }

            component.CollidingCount += 1;

            if (component.CollidingCount > 1)
            {
                // TODO: Try docking
                // check either side fixture on ours to see if it overlaps either side on theirs and then dock
            }
        }

        private void OnEndCollide(EntityUid uid, DockingComponent component, EndCollideEvent args)
        {
            if (component.DockedWith != null) return;

            if (!EntityManager.TryGetComponent(uid, out DockingComponent? otherDocking))
            {
                return;
            }

            component.CollidingCount -= 1;
            // TODO: Decrease if ending with another docking comp

            throw new System.NotImplementedException();
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
            component.CollidingCount = 0;

            if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? physicsComponent))
            {
                return;
            }

            // TODO: Break
            for (var i = 0; i < 4; i++)
            {
                _broadphaseSystem.DestroyFixture(physicsComponent, DockingFixture + i);
            }
        }

        private void EnableDocking(EntityUid uid, DockingComponent component)
        {
            if (component.Enabled) return;

            component.CollidingCount = 0;

            if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? physicsComponent))
            {
                return;
            }

            component.Enabled = true;
            var i = 0;

            // Make a fixture for each corner
            for (var x = -0.5f; x <= 0.5f; x += 1f)
            {
                for (var y = -0.5f; y <= 0.5f; y += 1f)
                {
                    var shape = new PhysShapeCircle
                    {
                        Position = new Vector2(x, y),
                        Radius = DockingRadius
                    };

                    // TODO: Its own collision mask / layer probably
                    var fixture = new Fixture(physicsComponent, shape)
                    {
                        ID = DockingFixture + i,
                        Hard = false,
                    };

                    _broadphaseSystem.CreateFixture(physicsComponent, fixture);
                    i += 1;
                }
            }
        }

        private void Dock(DockingComponent dockA, DockingComponent dockB)
        {

        }

        private void Undock(DockingComponent dock)
        {
            DebugTools.Assert(dock.DockedWith != null);
            // TODO: Break weld
            dock.CollidingCount = 0;
        }

        // So how docking works is that if 2 adjacent fixtures are colliding with 2 other docking fixtures the grids will "dock"
    }
}
