using Content.Server.Explosion.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using System;

namespace Content.Server.Explosion
{
    public class ProximityTriggerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TriggerOnProximityComponent, ComponentStartup>(Enable);
        }

        private void Enable(EntityUid uid, TriggerOnProximityComponent component, ComponentStartup args)
        {
            component.Enabled = true;
        }

        public void SetProximityFixture(EntityUid uid, TriggerOnProximityComponent component, bool remove)
        {
            var entity = EntityManager.GetEntity(uid);
            var broadphase = Get<SharedBroadphaseSystem>();
            entity.EnsureComponent<PhysicsComponent>();

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
                        broadphase.CreateFixture(physics, new Fixture(physics, component.Shape) {CollisionMask = 3, Hard = false, ID = component.ProximityFixture });
                        
                }
            }
        }
    }
}
