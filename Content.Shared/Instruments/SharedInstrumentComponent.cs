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

    [DataField("program")]
    public byte InstrumentProgram { get; set; }

    [DataField("bank")]
    public byte InstrumentBank { get; set; }

    [DataField]
    public bool AllowPercussion { get; set; }

    [DataField]
    public bool AllowProgramChange { get; set; }

    [DataField]
    public bool RespectMidiLimits { get; set; } = true;

    [DataField]
    public byte MinVolume { get; set; }

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Master { get; set; } = null;

    [ViewVariables]
    public BitArray FilteredChannels { get; set; } = new(RobustMidiEvent.MaxChannels, true);
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
    public BitArray UsedChannels = new(16, false);
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
    public byte MinVolume;

    public BitArray FilteredChannels = default!;
}

/// <summary>
///     This message is sent to the client to update midi min volume.
/// </summary>
[Serializable, NetSerializable]
public sealed class InstrumentSetMidiMinVolumeEvent : EntityEventArgs
{
    public NetEntity Uid { get; }
    public byte MinVolume { get; set; }

    public InstrumentSetMidiMinVolumeEvent(NetEntity uid, byte minVolume)
    {
        Uid = uid;
        MinVolume = minVolume;
    }
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
/// Sets the MIDI channels on an instrument.
/// </summary>
[Serializable, NetSerializable]
public sealed class InstrumentSetChannelsEvent : EntityEventArgs
{
    public NetEntity Uid { get; }
    public BitArray Channels { get; set; }

    public InstrumentSetChannelsEvent(NetEntity uid, BitArray channels)
    {
        Uid = uid;
        Channels = channels;
    }
}

/// <summary>
/// Represents a single midi track with the track name, instrument name and bank instrument name extracted.
/// </summary>
[Obsolete("Use MidiInfo instead.")]
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
    /// The first program change resolved to the name.
    /// </summary>
    public string? ProgramName;

    public override string ToString()
    {
        return $"Track Name: {TrackName}; Instrument Name: {InstrumentName}; Program Name: {ProgramName}";
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

        if (ProgramName != null)
            ProgramName = Truncate(ProgramName, limit);
    }

    public void SanitizeFields()
    {
        if (InstrumentName != null)
            InstrumentName = Sanitize(InstrumentName);

        if (TrackName != null)
            TrackName = Sanitize(TrackName);

        if (ProgramName != null)
            ProgramName = Sanitize(ProgramName);
    }

    private const string Postfix = "…";
    // TODO: Make a general method to use in RT? idk if we have that.
    private static string Truncate(string input, int limit)
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

/// <summary>
/// Contains the header information from a MIDI file.
/// </summary>
[Serializable, NetSerializable]
public sealed class MidiHeaderInfo
{
    /// <summary>
    /// Used MIDI file container format.
    /// </summary>
    /// <remarks>
    /// 0: All data store inside one track.
    /// 1: Data stored across multiple co-dependent tracks meant to be played in sync.
    /// Tempo data must be stored in the first track.
    /// 2: Data stored across multiple independent tracks with their own starting points and tempos. (Rarely used)
    /// </remarks>
    public int Format;

    /// <summary>
    /// Amount of tracks detected inside the file (including invalid ones).
    /// </summary>
    public int NumTracks;

    /// <summary>
    /// Set to true if the MIDI files uses SMPTE timing instead of the more common ticks / quarter note
    /// </summary>
    /// <remarks>
    /// SMPTE is currently not supported by the parser.
    /// </remarks>
    public bool IsSmpte;

    /// <summary>
    /// Ticks / Quarter note also known as time base.
    /// </summary>
    public int TimeBase;
}

/// <summary>
/// Contains information about a single track inside a MIDI file.
/// </summary>
[Serializable, NetSerializable]
public sealed class MidiTrackInfo
{
    private const string Postfix = "…";

    /// <summary>
    /// Track length in bytes.
    /// </summary>
    public int Length;

    /// <summary>
    /// The parsed text field of track (if used)
    /// </summary>
    public string? Text;

    /// <summary>
    /// The parsed copyright field of track (if used)
    /// </summary>
    public string? Copyright;

    /// <summary>
    /// The parsed track name of track (if used)
    /// </summary>
    public string? TrackName;

    /// <summary>
    /// The parsed instrument name of track (if used)
    /// </summary>
    public string? InstrumentName;

    /// <summary>
    /// Contains all used channels used by this track.
    /// </summary>
    public BitArray UsedChannels = new(RobustMidiEvent.MaxChannels, false);

    /// <summary>
    /// Total length of this track in ticks.
    /// </summary>
    public int TotalTicks = 0;

    /// <summary>
    /// Contains all detected tempos as TickPosition, Tempo(microseconds per quarter note).
    /// </summary>
    public Dictionary<int, int> TempoMap = [];

    public override string ToString()
    {
        return $"MIDI Track: Name = {TrackName}, TotalTicks = {TotalTicks}";
    }

    /// <summary>
    /// Truncates the fields based on the limit inputted into this method.
    /// </summary>
    public void TruncateFields(int limit)
    {
        if (Text != null)
            Text = Truncate(Text, limit);

        if (Copyright != null)
            Copyright = Truncate(Copyright, limit);

        if (TrackName != null)
            TrackName = Truncate(TrackName, limit);

        if (InstrumentName != null)
            InstrumentName = Truncate(InstrumentName, limit);
    }

    public void SanitizeFields()
    {
        if (Text != null)
            Text = Sanitize(Text);

        if (Copyright != null)
            Copyright = Sanitize(Copyright);

        if (InstrumentName != null)
            InstrumentName = Sanitize(InstrumentName);

        if (TrackName != null)
            TrackName = Sanitize(TrackName);
    }

    // TODO: Still make a general method to use in RT. I, too, don't know if we have that.
    private static string Truncate(string input, int limit)
    {
        if (string.IsNullOrEmpty(input) || limit <= 0 || input.Length <= limit)
            return input;

        var truncatedLength = limit - Postfix.Length;

        return input.Substring(0, truncatedLength) + Postfix;
    }

    private static string Sanitize(string input)
    {
        var sanitized = new StringBuilder(input.Length);

        foreach (var c in input)
        {
            if (!char.IsControl(c) && c <= 127) // no control characters, only ASCII
                sanitized.Append(c);
        }

        return sanitized.ToString();
    }
}

/// <summary>
/// Contains information about a single midi file, includes header, tracks, used channels and calculated play time.
/// </summary>
/// <remarks>Does not contain the actual MIDI data needed to play it.</remarks>
[Serializable, NetSerializable]
public sealed class MidiFileInfo
{
    /// <summary>
    /// Header data of the MIDI file (format, timebase, etc.)
    /// </summary>
    public MidiHeaderInfo? Header;

    /// <summary>
    /// Collection of one or more tracks contained inside the file.
    /// </summary>
    public MidiTrackInfo[] Tracks = [];

    /// <summary>
    /// Contains all used channels across all MIDI tracks inside the file.
    /// </summary>
    public BitArray UsedChannels = new(RobustMidiEvent.MaxChannels, false);

    /// <summary>
    /// Calculated playtime of the complete MIDI file taking into account time base and tempo changes.
    /// </summary>
    public double PlayTimeMinutes;

    /// <summary>
    /// Song tick count measured across all tracks.
    /// </summary>
    public int TotalTicks;
}
