using Content.Server.Explosion.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Explosion
{
    public class ProximityTriggerSystem : EntitySystem
    {
        public void SetProximityFixture(EntityUid uid, TriggerOnProximityComponent component, bool remove)
        {
            var entity = EntityManager.GetEntity(uid);
            var broadphase = Get<SharedBroadphaseSystem>();

            if (entity.TryGetComponent(out PhysicsComponent? physics))
            {
                var fixture = physics.GetFixture(component.ProximityFixture);
                if (!remove)
                {   
                    if (fixture != null)
                        broadphase.DestroyFixture(physics, fixture);
                }
                else
                {
                    if (fixture == null)
                        broadphase.CreateFixture(physics, new Fixture(physics, component.Shape) { Hard = false, ID = component.ProximityFixture });
                }
            }
        }
    }
}
