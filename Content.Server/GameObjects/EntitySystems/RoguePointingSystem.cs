using Content.Server.GameObjects.Components.Pointing;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class RoguePointingSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var component in ComponentManager.EntityQuery<RoguePointingArrowComponent>(true))
            {
                component.Update(frameTime);
            }
        }
    }
}
