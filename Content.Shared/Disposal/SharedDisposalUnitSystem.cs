using Content.Shared.Disposal.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.Disposal
{
    [UsedImplicitly]
    public sealed class SharedDisposalUnitSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<SharedDisposalUnitComponent>(true))
            {
                comp.Update(frameTime);
            }

            foreach (var comp in ComponentManager.EntityQuery<SharedDisposalMailingUnitComponent>(true))
            {
                comp.Update(frameTime);
            }
        }
    }
}
