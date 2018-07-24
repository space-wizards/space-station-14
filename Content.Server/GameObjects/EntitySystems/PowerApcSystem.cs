using Content.Server.GameObjects.Components.Power;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;

namespace Content.Server.GameObjects.EntitySystems
{
    class PowerApcSystem : EntitySystem
    {
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(ApcComponent));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var comp = entity.GetComponent<ApcComponent>();
                comp.OnUpdate();
            }
        }
    }
}
