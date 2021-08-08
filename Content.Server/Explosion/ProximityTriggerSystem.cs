using Content.Server.Explosion.Components;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using System;

namespace Content.Server.Explosion
{
    public class ProximityTriggerSystem : EntitySystem
    {
        public void AddProximityFixture(EntityUid uid, TriggerOnProximityComponent component)
        {
            var entity = EntityManager.GetEntity(uid);
            var broadphase = Get<SharedBroadphaseSystem>();
            
            if (entity.TryGetComponent(out PhysicsComponent? physics))
            {
                var fixture = physics.GetFixture(component.ProximityFixture);
                if (fixture == null)
                    broadphase.CreateFixture(physics, new Fixture(physics, component.Shape) { Hard = false, ID = component.ProximityFixture });                
            }
        }
        public void RemoveProximityFixture(EntityUid uid, TriggerOnProximityComponent component)
        {
            var entity = EntityManager.GetEntity(uid);
            var broadphase = Get<SharedBroadphaseSystem>();

            if (entity.TryGetComponent(out PhysicsComponent? physics))
            {
                var fixture = physics.GetFixture(component.ProximityFixture);
                if (fixture != null)
                    broadphase.DestroyFixture(physics, fixture);
            }
        }
    }
}
