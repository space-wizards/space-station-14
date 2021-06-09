using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Metabolism
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
