using System.Collections.Generic;
using Content.Server.Shuttles.Components;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;

namespace Content.Server.Shuttles.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ShuttleSystem : EntitySystem
    {
        private const float TileMassMultiplier = 4f;

        public float ShuttleMaxLinearImpulse = 13f;
        public float ShuttleMaxAngularImpulse = 3f;

        public float ShuttleMovingLinearDamping = 1.15f;
        public float ShuttleIdleLinearDamping = 0.5f;

        public float ShuttleMovingAngularDamping = 3f;
        public float ShuttleIdleAngularDamping = 3f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ShuttleComponent, ComponentAdd>(OnShuttleAdd);
            SubscribeLocalEvent<ShuttleComponent, ComponentStartup>(OnShuttleStartup);
            SubscribeLocalEvent<ShuttleComponent, ComponentShutdown>(OnShuttleShutdown);

            SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
            SubscribeLocalEvent<GridFixtureChangeEvent>(OnGridFixtureChange);

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CCVars.ShuttleMaxLinearImpulse, SetShuttleMaxLinearImpulse, true);
            configManager.OnValueChanged(CCVars.ShuttleMaxAngularImpulse, SetShuttleMaxAngularImpulse, true);
            configManager.OnValueChanged(CCVars.ShuttleIdleLinearDamping, SetShuttleIdleLinearDamping, true);
            configManager.OnValueChanged(CCVars.ShuttleIdleAngularDamping, SetShuttleIdleAngularDamping, true);
            configManager.OnValueChanged(CCVars.ShuttleMovingLinearDamping, SetShuttleMovingLinearDamping, true);
            configManager.OnValueChanged(CCVars.ShuttleMovingAngularDamping, SetShuttleMovingAngularDamping, true);
        }

        private void SetShuttleMaxLinearImpulse(float value) => ShuttleMaxLinearImpulse = value;
        private void SetShuttleMaxAngularImpulse(float value) => ShuttleMaxAngularImpulse = value;
        private void SetShuttleIdleLinearDamping(float value) => ShuttleIdleLinearDamping = value;
        private void SetShuttleIdleAngularDamping(float value) => ShuttleIdleAngularDamping = value;
        private void SetShuttleMovingLinearDamping(float value) => ShuttleMovingLinearDamping = value;
        private void SetShuttleMovingAngularDamping(float value) => ShuttleMovingAngularDamping = value;

        public override void Shutdown()
        {
            base.Shutdown();
            var configManar = IoCManager.Resolve<IConfigurationManager>();
            configManar.UnsubValueChanged(CCVars.ShuttleMaxLinearImpulse, SetShuttleMaxLinearImpulse);
            configManar.UnsubValueChanged(CCVars.ShuttleMaxAngularImpulse, SetShuttleMaxAngularImpulse);
            configManar.UnsubValueChanged(CCVars.ShuttleIdleLinearDamping, SetShuttleIdleLinearDamping);
            configManar.UnsubValueChanged(CCVars.ShuttleIdleAngularDamping, SetShuttleIdleAngularDamping);
            configManar.UnsubValueChanged(CCVars.ShuttleMovingLinearDamping, SetShuttleMovingLinearDamping);
            configManar.UnsubValueChanged(CCVars.ShuttleMovingAngularDamping, SetShuttleMovingAngularDamping);
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
            EntityManager.EnsureComponent<ShuttleComponent>(ev.EntityUid);
        }

        private void OnShuttleStartup(EntityUid uid, ShuttleComponent component, ComponentStartup args)
        {
            if (!EntityManager.HasComponent<IMapGridComponent>(component.Owner))
            {
                return;
            }

            if (!EntityManager.TryGetComponent(component.Owner, out PhysicsComponent? physicsComponent))
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
            if (!EntityManager.TryGetComponent(component.Owner, out PhysicsComponent? physicsComponent)) return;

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
            component.LinearDamping = ShuttleIdleLinearDamping;
            component.AngularDamping = ShuttleIdleAngularDamping;
        }

        private void Disable(PhysicsComponent component)
        {
            component.BodyType = BodyType.Static;
            component.BodyStatus = BodyStatus.OnGround;
            component.FixedRotation = true;
        }

        private void OnShuttleShutdown(EntityUid uid, ShuttleComponent component, ComponentShutdown args)
        {
            // None of the below is necessary for any cleanup if we're just deleting.
            if (EntityManager.GetComponent<MetaDataComponent>(uid).EntityLifeStage >= EntityLifeStage.Terminating) return;

            if (!EntityManager.TryGetComponent(component.Owner, out PhysicsComponent? physicsComponent))
            {
                return;
            }

            Disable(physicsComponent);

            if (!EntityManager.TryGetComponent(component.Owner, out FixturesComponent? fixturesComponent))
            {
                return;
            }

            foreach (var fixture in fixturesComponent.Fixtures.Values)
            {
                fixture.Mass = 0f;
            }
        }
    }
}
