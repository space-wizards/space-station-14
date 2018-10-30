using Content.Server.GameObjects.Components.Interactable;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    public class HandHeldLightSystem : EntitySystem
    {
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(HandheldLightComponent));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var comp = entity.GetComponent<HandheldLightComponent>();
                comp.OnUpdate(frameTime);
            }
        }
    }
}
