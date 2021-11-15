using Content.Server.Shuttles.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;

namespace Content.Server.Shuttles.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ShuttleSystem : EntitySystem
    {
        private const float TileMassMultiplier = 1f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ShuttleComponent, ComponentStartup>(HandleShuttleStartup);
            SubscribeLocalEvent<ShuttleComponent, ComponentShutdown>(HandleShuttleShutdown);

            SubscribeLocalEvent<GridInitializeEvent>(HandleGridInit);
            SubscribeLocalEvent<GridFixtureChangeEvent>(HandleGridFixtureChange);
        }

        private void HandleGridFixtureChange(GridFixtureChangeEvent args)
        {
            // Look this is jank but it's a placeholder until we design it.
            if (args.NewFixtures.Count == 0) return;

            foreach (var fixture in args.NewFixtures)
            {
                fixture.Mass = fixture.Area * TileMassMultiplier;
                fixture.Restitution = 0.1f;
            }
        }

        private void HandleGridInit(GridInitializeEvent ev)
        {
            EntityManager.GetEntity(ev.EntityUid).EnsureComponent<ShuttleComponent>();
        }

        private void HandleShuttleStartup(EntityUid uid, ShuttleComponent component, ComponentStartup args)
        {
            if (!component.Owner.HasComponent<IMapGridComponent>())
            {
                return;
            }

            if (!component.Owner.TryGetComponent(out PhysicsComponent? physicsComponent))
            {
                return;
            }

            if (component.Enabled)
            {
                Enable(physicsComponent);
            }
        }

        public void Toggle(ShuttleComponent component)
        {
            if (!component.Owner.TryGetComponent(out PhysicsComponent? physicsComponent)) return;

            component.Enabled = !component.Enabled;

            if (component.Enabled)
            {
                Enable(physicsComponent);
            }
            else
            {
                Disable(physicsComponent);
            }
        }

        private void Enable(PhysicsComponent component)
        {
            component.BodyType = BodyType.Dynamic;
            component.BodyStatus = BodyStatus.InAir;
            //component.FixedRotation = false; TODO WHEN ROTATING SHUTTLES FIXED.
            component.FixedRotation = false;
            component.LinearDamping = 0.2f;
            component.AngularDamping = 0.3f;
        }

        private void Disable(PhysicsComponent component)
        {
            component.BodyType = BodyType.Static;
            component.BodyStatus = BodyStatus.OnGround;
            component.FixedRotation = true;
        }

        private void HandleShuttleShutdown(EntityUid uid, ShuttleComponent component, ComponentShutdown args)
        {
            if (!component.Owner.TryGetComponent(out PhysicsComponent? physicsComponent))
            {
                return;
            }

            Disable(physicsComponent);

            foreach (var fixture in physicsComponent.Fixtures)
            {
                fixture.Mass = 0f;
            }
        }
    }
}
