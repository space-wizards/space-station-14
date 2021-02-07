using Content.Server.GameObjects.Components.Instruments;
using Content.Shared;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class InstrumentSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        public override void Initialize()
        {
            base.Initialize();

            _cfg.OnValueChanged(CCVars.MaxMidiEventsPerSecond, OnMaxMidiEventsPerSecondChanged, true);
            _cfg.OnValueChanged(CCVars.MaxMidiEventsPerBatch, OnMaxMidiEventsPerBatchChanged, true);
            _cfg.OnValueChanged(CCVars.MaxMidiBatchesDropped, OnMaxMidiBatchesDroppedChanged, true);
            _cfg.OnValueChanged(CCVars.MaxMidiLaggedBatches, OnMaxMidiLaggedBatchesChanged, true);
        }

        public int MaxMidiEventsPerSecond { get; private set; }
        public int MaxMidiEventsPerBatch { get; private set; }
        public int MaxMidiBatchesDropped { get; private set; }
        public int MaxMidiLaggedBatches { get; private set; }

        private void OnMaxMidiLaggedBatchesChanged(int obj)
        {
            MaxMidiLaggedBatches = obj;
        }

        private void OnMaxMidiBatchesDroppedChanged(int obj)
        {
            MaxMidiBatchesDropped = obj;
        }

        private void OnMaxMidiEventsPerBatchChanged(int obj)
        {
            MaxMidiEventsPerBatch = obj;
        }

        private void OnMaxMidiEventsPerSecondChanged(int obj)
        {
            MaxMidiEventsPerSecond = obj;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var component in ComponentManager.EntityQuery<InstrumentComponent>(true))
            {
                component.Update(frameTime);
            }
        }
    }
}
