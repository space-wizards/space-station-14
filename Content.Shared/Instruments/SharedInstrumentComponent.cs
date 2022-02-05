using System;
using Robust.Shared.Analyzers;
using Robust.Shared.Audio.Midi;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Instruments;

[NetworkedComponent, Friend(typeof(SharedInstrumentSystem))]
public class SharedInstrumentComponent : Component
{
    [ViewVariables]
    public bool Playing { get; set; }

    [ViewVariables]
    public uint LastSequencerTick { get; set; }

    [DataField("program"), ViewVariables(VVAccess.ReadWrite)]
    public byte InstrumentProgram { get; set; }

    [DataField("bank"), ViewVariables(VVAccess.ReadWrite)]
    public byte InstrumentBank { get; set; }

    [DataField("allowPercussion"), ViewVariables(VVAccess.ReadWrite)]
    public bool AllowPercussion { get; set; }

    [DataField("allowProgramChange"), ViewVariables(VVAccess.ReadWrite)]
    public bool AllowProgramChange { get ; set; }

    [DataField("respectMidiLimits"), ViewVariables(VVAccess.ReadWrite)]
    public bool RespectMidiLimits { get; set; } = true;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool DirtyRenderer { get; set; }
}


/// <summary>
///     This message is sent to the client to completely stop midi input and midi playback.
/// </summary>
[Serializable, NetSerializable]
public class InstrumentStopMidiEvent : EntityEventArgs
{
    public EntityUid Uid { get; }

    public InstrumentStopMidiEvent(EntityUid uid)
    {
        Uid = uid;
    }
}

/// <summary>
///     This message is sent to the client to start the synth.
/// </summary>
[Serializable, NetSerializable]
public class InstrumentStartMidiEvent : EntityEventArgs
{
    public EntityUid Uid { get; }

    public InstrumentStartMidiEvent(EntityUid uid)
    {
        Uid = uid;
    }
}

/// <summary>
///     This message carries a MidiEvent to be played on clients.
/// </summary>
[Serializable, NetSerializable]
public class InstrumentMidiEventEvent : EntityEventArgs
{
    public EntityUid Uid { get; }
    public MidiEvent[] MidiEvent { get; }

    public InstrumentMidiEventEvent(EntityUid uid, MidiEvent[] midiEvent)
    {
        Uid = uid;
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

    public InstrumentState(bool playing, byte instrumentProgram, byte instrumentBank, bool allowPercussion, bool allowProgramChange, bool respectMidiLimits, uint sequencerTick = 0)
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
