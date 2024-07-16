using Robust.Shared.GameStates;

namespace Content.Shared.TapeRecorder.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTapeRecorderSystem))]
public sealed partial class TapeCassetteComponent : Component
{
    /// <summary>
    /// A list of all recorded voice, containing timestamp, name and spoken words
    /// </summary>
    [DataField]
    public List<TapeCassetteRecordedMessage> RecordedData = new();

    /// <summary>
    /// The current position within the tape we are at, in seconds
    /// Only dirtied when the tape recorder is stopped
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float CurrentPosition = 0f;

    /// <summary>
    /// Maximum capacity of this tape
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MaxCapacity = TimeSpan.FromSeconds(120);

    /// <summary>
    /// How long to spool the tape after it was damaged
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RepairDelay = TimeSpan.FromSeconds(3);

    //Locale references
    [DataField]
    public LocId TextUnintelligable = "tape-recorder-voice-unintelligible";

    [DataField]
    public LocId TextCorruptionCharacter = "tape-recorder-message-corruption";

    [DataField]
    public LocId TextExamine = "tape-cassette-position";

    [DataField]
    public LocId TextDamaged = "tape-cassette-damaged";

    /// <summary>
    /// Temporary storage for all heard messages that need processing
    /// </summary>
    [DataField]
    public List<TapeCassetteRecordedMessage> Buffer = new();
}

/// <summary>
/// Removed from the cassette when damaged to prevent it being played until repaired
/// </summary>
[RegisterComponent]
public sealed partial class FitsInTapeRecorderComponent : Component
{
}
