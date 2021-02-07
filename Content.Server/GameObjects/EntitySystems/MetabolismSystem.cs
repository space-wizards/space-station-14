using Content.Server.GameObjects.Components.Metabolism;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class MetabolismSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var metabolism in ComponentManager.EntityQuery<MetabolismComponent>(true))
            {
                metabolism.Update(frameTime);
            }
        }
    }
}
