using Content.Server.GameObjects.Components.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    public class LatheSystem : EntitySystem
    {
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(LatheComponent));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var comp = entity.GetComponent<LatheComponent>();
                if (comp.Producing == false && comp.Queue.Count > 0)
                {
                    comp.Produce(comp.Queue.Dequeue());
                }
            }
        }
    }
}
