namespace Content.Shared.TapeRecorder.Components;

[RegisterComponent]
public sealed partial class TapeCassetteComponent : Component
{
    /// <summary>
    /// A list of all recorded voice, containing timestamp, name and spoken words
    /// </summary>
    public List<TapeCassetteRecordedMessage> RecordedData { get; set; } = new List<TapeCassetteRecordedMessage>();

    /// <summary>
    /// The current position within the tape we are at, in seconds
    /// </summary>
    public float CurrentPosition { get; set; } = 0f;

    /// <summary>
    /// Maximum capacity of this tape, in seconds
    /// </summary>
    [DataField("maxCapacity")]
    public float MaxCapacity { get; set; } = 120f;

    /// <summary>
    /// How long to spool the tape after it was damaged
    /// </summary>
    [DataField("repairDelay")]
    public float RepairDelay { get; set; } = 3f;
}
