
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Content.Client.GameObjects.Components.Interactable;

namespace Content.Client.GameObjects.EntitySystems
{
    public class GlowstickSystem : EntitySystem
    {
        public override void FrameUpdate(float frameTime)
        {
            foreach (var glowstick in ComponentManager.EntityQuery<GlowstickComponent>())
            {
                glowstick.Update(frameTime);
            }
        }
    }
}
