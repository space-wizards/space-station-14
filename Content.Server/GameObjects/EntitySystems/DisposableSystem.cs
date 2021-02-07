using Content.Server.GameObjects.Components.Disposal;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class DisposableSystem : EntitySystem
    {

        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<DisposalHolderComponent>(true))
            {
                comp.Update(frameTime);
            }
        }
    }
}
