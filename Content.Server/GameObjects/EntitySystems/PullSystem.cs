using Content.Server.GameObjects.Components.Movement;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class PullSystem: EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery<PullableComponent>();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var entity in RelevantEntities)
            {
                entity.GetComponent<PullableComponent>().Update();
            }
        }
    }
}
