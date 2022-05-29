using Content.Shared.CCVar;

namespace Content.Server.Instruments;

public sealed partial class InstrumentSystem
{
    public int MaxMidiEventsPerSecond { get; private set; }
    public int MaxMidiEventsPerBatch { get; private set; }
    public int MaxMidiBatchesDropped { get; private set; }
    public int MaxMidiLaggedBatches { get; private set; }

    private void InitializeCVars()
    {
        _cfg.OnValueChanged(CCVars.MaxMidiEventsPerSecond, OnMaxMidiEventsPerSecondChanged, true);
        _cfg.OnValueChanged(CCVars.MaxMidiEventsPerBatch, OnMaxMidiEventsPerBatchChanged, true);
        _cfg.OnValueChanged(CCVars.MaxMidiBatchesDropped, OnMaxMidiBatchesDroppedChanged, true);
        _cfg.OnValueChanged(CCVars.MaxMidiLaggedBatches, OnMaxMidiLaggedBatchesChanged, true);
    }

    private void ShutdownCVars()
    {
        _cfg.UnsubValueChanged(CCVars.MaxMidiEventsPerSecond, OnMaxMidiEventsPerSecondChanged);
        _cfg.UnsubValueChanged(CCVars.MaxMidiEventsPerBatch, OnMaxMidiEventsPerBatchChanged);
        _cfg.UnsubValueChanged(CCVars.MaxMidiBatchesDropped, OnMaxMidiBatchesDroppedChanged);
        _cfg.UnsubValueChanged(CCVars.MaxMidiLaggedBatches, OnMaxMidiLaggedBatchesChanged);
    }

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
}
