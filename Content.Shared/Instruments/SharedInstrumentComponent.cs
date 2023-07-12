using System.Collections;
using Robust.Shared.Audio.Midi;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Instruments;

[NetworkedComponent]
[AutoGenerateComponentState(true)]
[Access(typeof(SharedInstrumentSystem))]
public abstract partial class SharedInstrumentComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public bool Playing { get; set; }

    [DataField("program"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public byte InstrumentProgram { get; set; }

    [DataField("bank"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public byte InstrumentBank { get; set; }

    [DataField("allowPercussion"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool AllowPercussion { get; set; }

    [DataField("allowProgramChange"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool AllowProgramChange { get ; set; }

    [DataField("respectMidiLimits"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool RespectMidiLimits { get; set; } = true;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? Master { get; set; } = null;

    [ViewVariables, AutoNetworkedField]
    public BitArray FilteredChannels { get; set; } = new(RobustMidiEvent.MaxChannels, true);
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
///     Send from the client to the server to set a master instrument.
/// </summary>
[Serializable, NetSerializable]
public sealed class InstrumentSetMasterEvent : EntityEventArgs
{
    public EntityUid Uid { get; }
    public EntityUid? Master { get; }

    public InstrumentSetMasterEvent(EntityUid uid, EntityUid? master)
    {
        Uid = uid;
        Master = master;
    }
}

/// <summary>
///     Send from the client to the server to set a master instrument channel.
/// </summary>
[Serializable, NetSerializable]
public sealed class InstrumentSetFilteredChannelEvent : EntityEventArgs
{
    public EntityUid Uid { get; }
    public int Channel { get; }
    public bool Value { get; }

    public InstrumentSetFilteredChannelEvent(EntityUid uid, int channel, bool value)
    {
        Uid = uid;
        Channel = channel;
        Value = value;
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

[NetSerializable, Serializable]
public enum InstrumentUiKey
{
    Key,
}
