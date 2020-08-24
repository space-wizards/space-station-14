
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Content.Server.GameObjects.Components.Interactable;

namespace Content.Server.GameObjects.EntitySystems
{
    public class GlowstickSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var glowstick in ComponentManager.EntityQuery<GlowstickComponent>())
            {
                glowstick.Update(frameTime);
            }
        }
    }
}
