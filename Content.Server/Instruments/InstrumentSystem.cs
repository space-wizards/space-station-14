using Content.Shared;
using Content.Shared.CCVar;
using Content.Server.UserInterface;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Instruments
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

            SubscribeLocalEvent<InstrumentComponent, ActivatableUIPlayerChangedEvent>(InstrumentNeedsClean);
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

        private void InstrumentNeedsClean(EntityUid uid, InstrumentComponent component, ActivatableUIPlayerChangedEvent ev)
        {
            component.Clean();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var component in EntityManager.EntityQuery<InstrumentComponent>(true))
            {
                component.Update(frameTime);
            }
        }
    }
}
