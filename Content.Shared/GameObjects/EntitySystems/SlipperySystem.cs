using Content.Shared.GameObjects.Components.Movement;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Shared.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class SlipperySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(SharedSlipperyComponent));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var entity in RelevantEntities)
            {
                entity.GetComponent<SharedSlipperyComponent>().Update(frameTime);
            }
        }
    }
}
