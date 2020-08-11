using Content.Server.GameObjects.Components.Disposal;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class DisposalUnitSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(DisposalUnitComponent));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                entity.GetComponent<DisposalUnitComponent>().Update(frameTime);
            }
        }
    }
}
