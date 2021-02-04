using Content.Server.GameObjects.Components.Recycling;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class RecyclerSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var component in ComponentManager.EntityQuery<RecyclerComponent>(true))
            {
                component.Update(frameTime);
            }
        }
    }
}
