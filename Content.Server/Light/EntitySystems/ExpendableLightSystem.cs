using Content.Server.Light.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Light.EntitySystems
{
    [UsedImplicitly]
    public class ExpendableLightSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var light in EntityManager.EntityQuery<ExpendableLightComponent>(true))
            {
                light.Update(frameTime);
            }
        }
    }
}
