using Content.Server.GameObjects.Components.Conveyor;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class ConveyorSystem : EntitySystem
    {
        private uint _nextId;

        public uint NextId()
        {
            return ++_nextId;
        }

        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(ConveyorComponent));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                if (!entity.TryGetComponent(out ConveyorComponent conveyor))
                {
                    continue;
                }

                conveyor.Update(frameTime);
            }
        }
    }
}
