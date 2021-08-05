using Content.Server.Explosion.Components;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Explosion
{
    class ProximityTriggerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TriggerOnProximityComponent, ComponentAdd>(AddFixture);
            SubscribeLocalEvent<TriggerOnProximityComponent, ComponentRemove>(DeleteFixture);
        }
        private void DeleteFixture(EntityUid uid, TriggerOnProximityComponent component, ComponentRemove args)
        {
            var entity = EntityManager.GetEntity(uid);
            entity.EnsureComponent<PhysicsComponent>();
            var physics = entity.GetComponent<PhysicsComponent>();
            var fixture = physics.GetFixture(component.ProximityFixture);

            if (fixture == null)
            {
                Logger.Error($"Tried to remove proximity trigger fixture for {component.Owner} but none found?");
                return;
            }

            Get<SharedBroadphaseSystem>().DestroyFixture(physics, fixture);
        }

        private void AddFixture(EntityUid uid, TriggerOnProximityComponent component, ComponentAdd args)
        {
            var entity = EntityManager.GetEntity(uid);
            entity.EnsureComponent<PhysicsComponent>();
            var physics = entity.GetComponent<PhysicsComponent>();
            if (physics.GetFixture(component.ProximityFixture) != null)
            {
                Logger.Error($"Found existing proximity trigger fixture on {component.Owner}");
                return;
            }
            Get<SharedBroadphaseSystem>().CreateFixture(physics, new Fixture(physics, component.Shape) { CollisionLayer = (int) CollisionGroup.ThrownItem, Hard = false, ID = component.ProximityFixture });
        }
    }
}
