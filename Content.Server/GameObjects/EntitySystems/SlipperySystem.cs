using Content.Server.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    public class SlipperySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(SlipperyComponent));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var entity in RelevantEntities)
            {
                entity.GetComponent<SlipperyComponent>().Update(frameTime);
            }
        }
    }
}
