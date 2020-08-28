
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Content.Client.GameObjects.Components;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class LightBehaviourSystem : EntitySystem
    {
        public override void FrameUpdate(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<LightBehaviourComponent>())
            {
                comp.Update(frameTime);
            }
        }
    }
}
