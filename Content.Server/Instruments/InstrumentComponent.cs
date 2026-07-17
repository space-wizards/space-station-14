using Content.Shared.Instruments;

namespace Content.Server.Instruments;

[RegisterComponent]
public sealed partial class InstrumentComponent : SharedInstrumentComponent
{
    [ViewVariables] public float Timer = 0f;
    [ViewVariables] public int BatchesDropped = 0;
    [ViewVariables] public int LaggedBatches = 0;
    [ViewVariables] public int MidiEventCount = 0;
    [ViewVariables] public uint LastSequencerTick = 0;
}
