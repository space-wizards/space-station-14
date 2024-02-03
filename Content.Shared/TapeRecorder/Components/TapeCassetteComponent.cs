using Robust.Shared.GameStates;

namespace Content.Shared.TapeRecorder.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
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
    /// Networked for client side prediction
    /// </summary>
    //Annoyingly im seeing a 8 - 10% discrepency between client server frame times - as such this needs to be auto networked
    [DataField]
    [AutoNetworkedField]
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

    [DataField]
    public LocId Unintelligable = "tape-recorder-voice-unintelligible";

    [DataField]
    public LocId CorruptionCharacter = "tape-recorder-message-corruption";
}

/// <summary>
/// Removed from the cassette when damaged to prevent it being played until repaired
/// </summary>
[RegisterComponent]
public sealed partial class FitsInTapeRecorderComponent : Component
{
}
