using System;
using Content.Shared.Shuttles;
using JetBrains.Annotations;
using Robust.Server.Physics;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;

namespace Content.Server.Shuttles
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

            var body = args.NewFixtures[0].Body;

            foreach (var fixture in args.NewFixtures)
            {
                fixture.Mass = fixture.Area * TileMassMultiplier;
                fixture.Restitution = 0.1f;
            }

            if (body.Owner.TryGetComponent(out ShuttleComponent? shuttleComponent))
            {
                RecalculateSpeedMultiplier(shuttleComponent, body);
            }

        }

        private void HandleGridInit(GridInitializeEvent ev)
        {
            EntityManager.GetEntity(ev.EntityUid).EnsureComponent<ShuttleComponent>();
        }

        /// <summary>
        /// Cache the thrust available to this shuttle.
        /// </summary>
        private void RecalculateSpeedMultiplier(SharedShuttleComponent shuttle, PhysicsComponent physics)
        {
            // TODO: Need per direction speed (Never Eat Soggy Weetbix).
            // TODO: This will need hella tweaking.
            var thrusters = physics.FixtureCount;

            if (thrusters == 0)
            {
                shuttle.SpeedMultipler = 0f;
            }

            const float ThrustPerThruster = 0.25f;
            const float MinimumThrustRequired = 0.005f;

            // Just so someone can't slap a single thruster on a station and call it a day; need to hit a minimum amount.
            var thrustRatio = Math.Max(0, thrusters * ThrustPerThruster / physics.Mass);

            if (thrustRatio < MinimumThrustRequired)
                shuttle.SpeedMultipler = 0f;

            const float MaxThrust = 10f;
            // This doesn't need to align with MinimumThrustRequired; if you set this higher it just means the first few additional
            // thrusters won't do anything.
            const float MinThrust = MinimumThrustRequired;

            shuttle.SpeedMultipler = MathF.Max(MinThrust, MathF.Min(MaxThrust, thrustRatio));
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

            if (component.Owner.TryGetComponent(out ShuttleComponent? shuttleComponent))
            {
                RecalculateSpeedMultiplier(shuttleComponent, physicsComponent);
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
