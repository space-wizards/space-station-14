using Content.Server.GameObjects.Components.Instruments;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    public class InstrumentSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            EntityQuery = new TypeEntityQuery(typeof(InstrumentComponent));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var entity in RelevantEntities)
            {
                entity.GetComponent<InstrumentComponent>().Update(frameTime);
            }
        }
    }
}
