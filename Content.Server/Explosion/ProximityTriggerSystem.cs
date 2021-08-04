using Content.Server.Explosion.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Explosion
{
    class ProximityTriggerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();


            SubscribeLocalEvent<TriggerOnProximityComponent, ComponentRemove>(DeleteFixture);
        }
        private void DeleteFixture(EntityUid uid, TriggerOnProximityComponent component, ComponentRemove args)
        {
            var entity = EntityManager.GetEntity(uid);
            entity.EnsureComponent<PhysicsComponent>();
            var physics = entity.GetComponent<PhysicsComponent>();
                
        }
    }
}
