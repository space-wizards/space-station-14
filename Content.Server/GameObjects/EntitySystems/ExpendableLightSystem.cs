using Content.Server.GameObjects.Components.Interactable;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class ExpendableLightSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var light in ComponentManager.EntityQuery<ExpendableLightComponent>(true))
            {
                light.Update(frameTime);
            }
        }
    }
}
