using System;
using Content.Shared.BodySystem;
using Robust.Shared.Audio.Midi;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Instruments
{
    public class SharedInstrumentComponent : Component
    {
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
        public uint SequencerTick { get; }

        public InstrumentState(bool playing, uint sequencerTick = 0) : base(ContentNetIDs.INSTRUMENTS)
        {
            Playing = playing;
            SequencerTick = sequencerTick;
        }
    }

    [NetSerializable, Serializable]
    public enum InstrumentUiKey
    {
        Key,
    }
}
