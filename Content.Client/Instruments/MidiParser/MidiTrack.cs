namespace Content.Client.Instruments.MidiParser;

/// <summary>
/// Represents a single midi track with the track name, instrument name and bank instrument name extracted.
/// </summary>
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
}
