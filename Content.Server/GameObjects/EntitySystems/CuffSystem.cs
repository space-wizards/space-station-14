using Content.Server.GameObjects.Components.ActionBlocking;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class CuffSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<CuffableComponent>())
            {
                comp.Update(frameTime);
            }
        }
    }
}
