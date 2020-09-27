
using Content.Server.GameObjects.Components.Interactable;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    public class ExpendableLightSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var light in ComponentManager.EntityQuery<ExpendableLightComponent>())
            {
                light.Update(frameTime);
            }
        }
    }
}
