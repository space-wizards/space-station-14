using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    internal class BatteryStorageSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IPauseManager _pauseManager;
#pragma warning restore 649

        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(BatteryStorageComponent));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                if (_pauseManager.IsEntityPaused(entity))
                {
                    continue;
                }
                entity.GetComponent<BatteryStorageComponent>().Update(frameTime);
            }
        }
    }
}
