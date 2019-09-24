using System;
using Robust.Shared.Audio.Midi;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Instruments
{
    public class SharedInstrumentComponent : Component
    {
        public override string Name => "Instrument";
        public override uint? NetID => ContentNetIDs.INSTRUMENTS;
    }


    [Serializable, NetSerializable]
    public class InstrumentStopMidiMessage : ComponentMessage
    {
    }

    [Serializable, NetSerializable]
    public class InstrumentMidiEventMessage : ComponentMessage
    {
        public MidiEvent MidiEvent;

        public InstrumentMidiEventMessage(MidiEvent midiEvent)
        {
            MidiEvent = midiEvent;
        }
    }

    [NetSerializable, Serializable]
    public enum InstrumentUiKey
    {
        Key,
    }
}
