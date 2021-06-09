using Content.Server.Kitchen.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Kitchen.EntitySystems
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
