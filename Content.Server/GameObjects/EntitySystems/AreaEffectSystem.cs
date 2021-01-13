using Content.Server.GameObjects.Components.Chemistry;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class AreaEffectSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var inception in ComponentManager.EntityQuery<AreaEffectInceptionComponent>())
            {
                inception.InceptionUpdate(frameTime);
            }
        }
    }
}
