using Content.Server.GameObjects.Components.Interactable;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class HandHeldLightSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<HandheldLightComponent>())
            {
                if (comp.Owner.Paused) continue;
                comp.OnUpdate(frameTime);
            }
        }
    }
}
