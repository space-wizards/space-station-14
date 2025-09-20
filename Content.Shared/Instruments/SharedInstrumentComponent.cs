using System.Collections;
using System.Text;
using Robust.Shared.Audio.Midi;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Instruments;

[NetworkedComponent]
[Access(typeof(SharedInstrumentSystem))]
public abstract partial class SharedInstrumentComponent : Component
{
    [ViewVariables]
    public bool Playing { get; set; }

    [DataField("default")]
    public Instrument Instrument;

    [DataField]
    public bool AllowPercussion { get; set; }

    [DataField]
    public bool AllowProgramChange { get; set; }

    [DataField]
    public bool RespectMidiLimits { get; set; } = true;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Master { get; set; } = null;

    [ViewVariables]
    public BitArray FilteredChannels { get; set; } = new(RobustMidiEvent.MaxChannels, true);

    /// <summary>
    /// Stores the different instruments that can be swapped between.
    /// </summary>
    [DataField("instrumentList")]
    public Dictionary<string, Instrument> Instruments = new();
}

/// <summary>
/// Defines information about the MIDI mapping of an instrument.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class Instrument
{
    /// <summary>
    /// The bank - the list of instruments - this instrument is in.
    /// If the instrument is not in a custom soundfont file, it's in bank 0.
    /// </summary>
    [DataField]
    public byte Bank;

    /// <summary>
    /// The ID of the instrument.
    /// </summary>
    [DataField]
    public byte Program;
}

/// <summary>
/// Component that indicates that musical instrument was activated (ui opened).
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class ActiveInstrumentComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public MidiTrack?[] Tracks = [];
}

[Serializable, NetSerializable]
public sealed class InstrumentComponentState : ComponentState
{
    public bool Playing;

    public byte InstrumentProgram;

    public byte InstrumentBank;

    public bool AllowPercussion;

    public bool AllowProgramChange;

    public bool RespectMidiLimits;

    public NetEntity? Master;

    public BitArray FilteredChannels = default!;
}

/// <summary>
///     This message is sent to the client to completely stop midi input and midi playback.
/// </summary>
[Serializable, NetSerializable]
public sealed class InstrumentStopMidiEvent : EntityEventArgs
{
    public NetEntity Uid { get; }

    public InstrumentStopMidiEvent(NetEntity uid)
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
    public NetEntity Uid { get; }
    public NetEntity? Master { get; }

    public InstrumentSetMasterEvent(NetEntity uid, NetEntity? master)
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
    public NetEntity Uid { get; }
    public int Channel { get; }
    public bool Value { get; }

    public InstrumentSetFilteredChannelEvent(NetEntity uid, int channel, bool value)
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
    public NetEntity Uid { get; }

    public InstrumentStartMidiEvent(NetEntity uid)
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
    public NetEntity Uid { get; }
    public RobustMidiEvent[] MidiEvent { get; }

    public InstrumentMidiEventEvent(NetEntity uid, RobustMidiEvent[] midiEvent)
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

/// <summary>
/// Sets the MIDI tracks on an Instrument.
/// </summary>
[Serializable, NetSerializable]
public sealed class InstrumentSetTracksEvent : EntityEventArgs
{
    public NetEntity Uid { get; }
    public MidiTrack?[] Tracks { get; set; }

    public InstrumentSetTracksEvent(NetEntity uid, MidiTrack?[] tracks)
    {
        Uid = uid;
        Tracks = tracks;
    }
}

/// <summary>
/// Represents a single midi track with the track name, instrument name and bank instrument name extracted.
/// </summary>
[Serializable, NetSerializable]
public sealed class MidiTrack
{
    /// <summary>
    /// The first specified Track Name
    /// </summary>
    public string? TrackName;

    /// <summary>
    /// The first specified instrument name
    /// </summary>
    public string? InstrumentName;

    /// <summary>
    /// The channels that this track targets.
    /// </summary>
    public Dictionary<byte, string> Channels = [];

    public override string ToString()
    {
        return $"Track Name: {TrackName}; Instrument Name: {InstrumentName}";
    }

    /// <summary>
    /// Truncates the fields based on the limit inputted into this method.
    /// </summary>
    public void TruncateFields(int limit)
    {
        if (InstrumentName != null)
            InstrumentName = Truncate(InstrumentName, limit);

        if (TrackName != null)
            TrackName = Truncate(TrackName, limit);
    }

    public void SanitizeFields()
    {
        if (InstrumentName != null)
            InstrumentName = Sanitize(InstrumentName);

        if (TrackName != null)
            TrackName = Sanitize(TrackName);
    }

    private const string Postfix = "…";

    // TODO: Make a general method to use in RT? idk if we have that.
    private string Truncate(string input, int limit)
    {
        if (string.IsNullOrEmpty(input) || limit <= 0 || input.Length <= limit)
            return input;

        var truncatedLength = limit - Postfix.Length;

        return input.Substring(0, truncatedLength) + Postfix;
    }

    private static string Sanitize(string input)
    {
        var sanitized = new StringBuilder(input.Length);

        foreach (char c in input)
        {
            if (!char.IsControl(c) && c <= 127) // no control characters, only ASCII
                sanitized.Append(c);
        }

        return sanitized.ToString();
    }
}
