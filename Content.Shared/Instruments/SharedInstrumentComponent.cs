using Robust.Shared.Audio.Midi;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Instruments;

[NetworkedComponent, Access(typeof(SharedInstrumentSystem))]
public abstract class SharedInstrumentComponent : Component
{
    [ViewVariables]
    public bool Playing { get; set; }

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
    [Access(typeof(SharedInstrumentSystem), Other = AccessPermissions.ReadWrite)] // FIXME Friends
    public bool DirtyRenderer { get; set; }
}


/// <summary>
///     This message is sent to the client to completely stop midi input and midi playback.
/// </summary>
[Serializable, NetSerializable]
public sealed class InstrumentStopMidiEvent : EntityEventArgs
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
public sealed class InstrumentStartMidiEvent : EntityEventArgs
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
public sealed class InstrumentMidiEventEvent : EntityEventArgs
{
    public EntityUid Uid { get; }
    public RobustMidiEvent[] MidiEvent { get; }

    public InstrumentMidiEventEvent(EntityUid uid, RobustMidiEvent[] midiEvent)
    {
        Uid = uid;
        MidiEvent = midiEvent;
    }
}

[Serializable, NetSerializable]
public sealed class InstrumentState : ComponentState
{
    public bool Playing { get; }
    public byte InstrumentProgram { get; }
    public byte InstrumentBank { get; }
    public bool AllowPercussion { get; }
    public bool AllowProgramChange { get; }
    public bool RespectMidiLimits { get; }

    public InstrumentState(bool playing, byte instrumentProgram, byte instrumentBank, bool allowPercussion, bool allowProgramChange, bool respectMidiLimits)
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
