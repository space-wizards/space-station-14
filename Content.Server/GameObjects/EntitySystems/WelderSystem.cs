using Content.Server.GameObjects.Components.Interactable.Tools;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;

namespace Content.Server.GameObjects.EntitySystems
{
    class WelderSystem : EntitySystem
    {
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(WelderComponent));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var comp = entity.GetComponent<WelderComponent>();
                comp.OnUpdate(frameTime);
            }
        }
    }
}
