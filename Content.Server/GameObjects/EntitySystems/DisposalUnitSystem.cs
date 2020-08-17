using Content.Server.GameObjects.Components.Disposal;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class DisposalUnitSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<DisposalUnitComponent>())
            {
                comp.Update(frameTime);
            }
        }
    }
}
