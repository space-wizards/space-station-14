using Content.Server.Disposal.Unit.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Disposal.Unit.EntitySystems
{
    [UsedImplicitly]
    internal sealed class DisposableSystem : EntitySystem
    {

        public override void Update(float frameTime)
        {
            foreach (var comp in EntityManager.EntityQuery<DisposalHolderComponent>(true))
            {
                comp.Update(frameTime);
            }
        }
    }
}
