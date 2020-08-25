
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Content.Client.GameObjects.Components.Interactable;

namespace Content.Client.GameObjects.EntitySystems
{
    public class ExpendableLightSystem : EntitySystem
    {
        public override void FrameUpdate(float frameTime)
        {
            foreach (var light in ComponentManager.EntityQuery<ExpendableLightComponent>())
            {
                light.Update(frameTime);
            }
        }
    }
}
