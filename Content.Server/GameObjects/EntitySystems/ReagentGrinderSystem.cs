using Content.Server.GameObjects.Components.Kitchen;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ReagentGrinderSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var comp in ComponentManager.EntityQuery<ReagentGrinderComponent>(true))
            {
                comp.OnUpdate();
            }
        }
    }
}
