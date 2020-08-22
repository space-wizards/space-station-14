using Content.Server.GameObjects.Components.Disposal;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class DisposableSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(DisposalHolderComponent));
        }

        public override void Update(float frameTime)
        {
            foreach (var disposable in RelevantEntities)
            {
                disposable.GetComponent<DisposalHolderComponent>().Update(frameTime);
            }
        }
    }
}
