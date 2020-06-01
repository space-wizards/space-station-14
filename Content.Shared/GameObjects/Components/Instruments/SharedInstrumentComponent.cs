using System;
using Content.Shared.BodySystem;
using Robust.Shared.Audio.Midi;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Instruments
{
    public class SharedInstrumentComponent : Component
    {

        // These 2 values are quite high for now, and this could be easily abused. Change this if people are abusing it.
        public const int MaxMidiEventsPerSecond = 1000;
        public const int MaxMidiEventsPerBatch = 60;
        public const int MaxMidiBatchDropped = 1;
        public const int MaxMidiLaggedBatches = 8;

        public override string Name => "Instrument";
        public override uint? NetID => ContentNetIDs.INSTRUMENTS;

        public virtual void Update(float delta)
        {
        }
    }


    /// <summary>
    ///     This message is sent to the client to completely stop midi input and midi playback.
    /// </summary>
    [Serializable, NetSerializable]
    public class InstrumentStopMidiMessage : ComponentMessage
    {
    }

    /// <summary>
    ///     This message is sent to the client to start the synth.
    /// </summary>
    [Serializable, NetSerializable]
    public class InstrumentStartMidiMessage : ComponentMessage
    {

    }

    /// <summary>
    ///     This message carries a MidiEvent to be played on clients.
    /// </summary>
    [Serializable, NetSerializable]
    public class InstrumentMidiEventMessage : ComponentMessage
    {
        public MidiEvent[] MidiEvent;

        public InstrumentMidiEventMessage(MidiEvent[] midiEvent)
        {
            MidiEvent = midiEvent;
        }
    }

    [Serializable, NetSerializable]
    public class InstrumentState : ComponentState
    {
        public bool Playing { get; }

        public InstrumentState(bool playing, uint sequencerTick = 0) : base(ContentNetIDs.INSTRUMENTS)
        {
            Playing = playing;
        }
    }

    [NetSerializable, Serializable]
    public enum InstrumentUiKey
    {
        Key,
    }
}
