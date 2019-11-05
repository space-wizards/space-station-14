using Content.Client.GameObjects.Components.Mobs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class MeleeLungeSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery<MeleeLungeComponent>();
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            foreach (var entity in RelevantEntities)
            {
                entity.GetComponent<MeleeLungeComponent>().Update(frameTime);
            }
        }
    }
}
