using System;
using System.Collections.Generic;
using Content.Server.Shuttles.Components;
using Content.Shared.Shuttles.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ShuttleSystem : EntitySystem
    {
        private const float TileMassMultiplier = 1f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ShuttleComponent, ComponentAdd>(OnShuttleAdd);
            SubscribeLocalEvent<ShuttleComponent, ComponentStartup>(OnShuttleStartup);
            SubscribeLocalEvent<ShuttleComponent, ComponentShutdown>(OnShuttleShutdown);

            SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
            SubscribeLocalEvent<GridFixtureChangeEvent>(OnGridFixtureChange);
        }

        /// <summary>
        /// Considers a thrust direction as being active for visualization purposes.
        /// </summary>
        public void EnableThrustDirection(ShuttleComponent component, DirectionFlag direction)
        {
            if ((component.ThrustDirections & direction) != 0x0) return;

            component.ThrustDirections |= direction;
            var index = (int) Math.Log2((int) direction);

            foreach (var comp in component.LinearThrusters[index])
            {
                if (!EntityManager.TryGetComponent(comp.OwnerUid, out SharedAppearanceComponent? appearanceComponent))
                    continue;

                appearanceComponent.SetData(ThrusterVisualState.Thrusting, true);
            }
        }

        /// <summary>
        /// Disables a thrust direction for visualization purposes.
        /// </summary>
        public void DisableThrustDirection(ShuttleComponent component, DirectionFlag direction)
        {
            if ((component.ThrustDirections & direction) == 0x0) return;

            component.ThrustDirections &= ~direction;
            var index = (int) Math.Log2((int) direction);

            foreach (var comp in component.LinearThrusters[index])
            {
                if (!EntityManager.TryGetComponent(comp.OwnerUid, out SharedAppearanceComponent? appearanceComponent))
                    continue;

                appearanceComponent.SetData(ThrusterVisualState.Thrusting, false);
            }
        }

        public void DisableAllThrustDirections(ShuttleComponent component)
        {
            foreach (DirectionFlag dir in Enum.GetValues(typeof(DirectionFlag)))
            {
                DisableThrustDirection(component, dir);
            }

            DebugTools.Assert(component.ThrustDirections == DirectionFlag.None);
        }

        private void OnShuttleAdd(EntityUid uid, ShuttleComponent component, ComponentAdd args)
        {
            // Easier than doing it in the comp and they don't have constructors.
            for (var i = 0; i < component.LinearThrusters.Length; i++)
            {
                component.LinearThrusters[i] = new List<ThrusterComponent>();
            }
        }

        private void OnGridFixtureChange(GridFixtureChangeEvent args)
        {
            // Look this is jank but it's a placeholder until we design it.
            if (args.NewFixtures.Count == 0) return;

            foreach (var fixture in args.NewFixtures)
            {
                fixture.Mass = fixture.Area * TileMassMultiplier;
                fixture.Restitution = 0.1f;
            }
        }

        private void OnGridInit(GridInitializeEvent ev)
        {
            EntityManager.GetEntity(ev.EntityUid).EnsureComponent<ShuttleComponent>();
        }

        private void OnShuttleStartup(EntityUid uid, ShuttleComponent component, ComponentStartup args)
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

        private void OnShuttleShutdown(EntityUid uid, ShuttleComponent component, ComponentShutdown args)
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
