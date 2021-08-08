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
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TriggerOnProximityComponent, AlterProximityFixtureEvent>(AddFixture);
            SubscribeLocalEvent<TriggerOnProximityComponent, ComponentInit>(EnabledOnInit);
        }

        private void EnabledOnInit(EntityUid uid, TriggerOnProximityComponent component, ComponentInit args)
        {
            component.Enabled = true;
        }

        private void AddFixture(EntityUid uid, TriggerOnProximityComponent component, AlterProximityFixtureEvent args)
        {
            var entity = EntityManager.GetEntity(uid);
            var broadphase = Get<SharedBroadphaseSystem>();
            
            if (entity.TryGetComponent(out PhysicsComponent? physics))
            {
                var fixture = physics.GetFixture(component.ProximityFixture);
                if (!args.Remove)
                {
                    if (fixture == null)
                        broadphase.CreateFixture(physics, new Fixture(physics, component.Shape) { Hard = false, ID = component.ProximityFixture });
                }
                else
                {
                    if (fixture != null)
                        broadphase.DestroyFixture(physics, fixture);
                }
            }
        }

        public class AlterProximityFixtureEvent : EntityEventArgs
        {
            public bool Remove { get; set; }
            public AlterProximityFixtureEvent(bool remove)
            {
                Remove = remove;
            }
        }
    }
}
