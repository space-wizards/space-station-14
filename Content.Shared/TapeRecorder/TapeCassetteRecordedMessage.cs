namespace Content.Shared.TapeRecorder;

/// <summary>
/// Every chat event recorded on a tape is saved in this format
/// </summary>
[ImplicitDataDefinitionForInheritors]
public sealed partial class TapeCassetteRecordedMessage : IComparable<TapeCassetteRecordedMessage>
{
    /// <summary>
    /// Number of seconds since the start of the tape that this event was recorded at
    /// </summary>
    [DataField(required: true)]
    public float Timestamp = 0;

    /// <summary>
    /// The name of the entity that spoke
    /// </summary>
    [DataField]
    public string? Name;

    /// <summary>
    /// Default name as fallback
    /// </summary>
    [DataField]
    public LocId DefaultName = "tape-recorder-voice-unknown";

    /// <summary>
    /// What was spoken
    /// </summary>
    [DataField]
    public string Message = string.Empty;

    public TapeCassetteRecordedMessage(float timestamp, string name, string message)
    {
        Timestamp = timestamp;
        Name = name;
        Message = message;
    }

    public int CompareTo(TapeCassetteRecordedMessage? other)
    {
        if (other == null)
            return 0;

        return (int) (Timestamp - other.Timestamp);
    }
}
