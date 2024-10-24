using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.TapeRecorder.Components;

// TODO: add things client needs for ui to networked state
[RegisterComponent, NetworkedComponent, Access(typeof(SharedTapeRecorderSystem))]
[AutoGenerateComponentState]
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
    [DataField, AutoNetworkedField]
    public float CurrentPosition = 0f;

    /// <summary>
    /// Maximum capacity of this tape
    /// </summary>
    [DataField]
    public TimeSpan MaxCapacity = TimeSpan.FromSeconds(120);

    /// <summary>
    /// How long to spool the tape after it was damaged
    /// </summary>
    [DataField]
    public TimeSpan RepairDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// When an entry is damaged, the chance of each character being corrupted.
    /// </summary>
    [DataField]
    public float CorruptionChance = 0.25f;

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

    /// <summary>
    /// Whitelist for tools that can be used to respool a damaged tape.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist RepairWhitelist = new();
}

/// <summary>
/// Removed from the cassette when damaged to prevent it being played until repaired
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FitsInTapeRecorderComponent : Component;
