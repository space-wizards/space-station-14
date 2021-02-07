using Content.Client.GameObjects.Components.Instruments;
using Content.Shared;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class InstrumentSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        public override void Initialize()
        {
            base.Initialize();

            _cfg.OnValueChanged(CCVars.MaxMidiEventsPerBatch, OnMaxMidiEventsPerBatchChanged, true);
            _cfg.OnValueChanged(CCVars.MaxMidiEventsPerSecond, OnMaxMidiEventsPerSecondChanged, true);
        }

        public int MaxMidiEventsPerBatch { get; private set; }
        public int MaxMidiEventsPerSecond { get; private set; }

        private void OnMaxMidiEventsPerSecondChanged(int obj)
        {
            MaxMidiEventsPerSecond = obj;
        }

        private void OnMaxMidiEventsPerBatchChanged(int obj)
        {
            MaxMidiEventsPerBatch = obj;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!_gameTiming.IsFirstTimePredicted)
            {
                return;
            }

            foreach (var instrumentComponent in EntityManager.ComponentManager.EntityQuery<InstrumentComponent>(true))
            {
                instrumentComponent.Update(frameTime);
            }
        }
    }
}
