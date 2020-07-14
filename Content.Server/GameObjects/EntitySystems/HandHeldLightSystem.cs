using Content.Server.GameObjects.Components.Interactable;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
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
