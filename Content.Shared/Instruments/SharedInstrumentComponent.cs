#nullable enable
using System;
using Content.Shared.NetIDs;
using Robust.Shared.Audio.Midi;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Instruments
{
    public class SharedInstrumentComponent : Component
    {
        public override string Name => "Instrument";
        public override uint? NetID => ContentNetIDs.INSTRUMENTS;

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual byte InstrumentProgram { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual byte InstrumentBank { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual bool AllowPercussion { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual bool AllowProgramChange { get ; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual bool RespectMidiLimits { get; set; }

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
        public byte InstrumentProgram { get; }
        public byte InstrumentBank { get; }
        public bool AllowPercussion { get; }
        public bool AllowProgramChange { get; }
        public bool RespectMidiLimits { get; }

        public InstrumentState(bool playing, byte instrumentProgram, byte instrumentBank, bool allowPercussion, bool allowProgramChange, bool respectMidiLimits, uint sequencerTick = 0) : base(ContentNetIDs.INSTRUMENTS)
        {
            Playing = playing;
            InstrumentProgram = instrumentProgram;
            InstrumentBank = instrumentBank;
            AllowPercussion = allowPercussion;
            AllowProgramChange = allowProgramChange;
            RespectMidiLimits = respectMidiLimits;
        }
    }

    [NetSerializable, Serializable]
    public enum InstrumentUiKey
    {
        Key,
    }
}
