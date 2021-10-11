using Content.Server.Pointing.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Pointing.EntitySystems
{
    [UsedImplicitly]
    internal sealed class RoguePointingSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var component in EntityManager.EntityQuery<RoguePointingArrowComponent>(true))
            {
                component.Update(frameTime);
            }
        }
    }
}
