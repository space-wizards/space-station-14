using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    public class StunSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(StunnableComponent));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var entity in RelevantEntities)
            {
                entity.GetComponent<StunnableComponent>().Update(frameTime);
            }
        }
    }
}
