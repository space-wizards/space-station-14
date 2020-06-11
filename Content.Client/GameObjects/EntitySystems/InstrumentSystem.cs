using Content.Client.GameObjects.Components.Instruments;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class InstrumentSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default;

        public override void Initialize()
        {
            base.Initialize();
            EntityQuery = new TypeEntityQuery(typeof(InstrumentComponent));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!_gameTiming.IsFirstTimePredicted)
            {
                return;
            }

            foreach (var entity in RelevantEntities)
            {
                entity.GetComponent<InstrumentComponent>().Update(frameTime);
            }
        }
    }
}
