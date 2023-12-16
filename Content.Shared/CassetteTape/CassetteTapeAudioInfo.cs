using Robust.Shared.Serialization;
namespace Content.Shared.CassetteTape;

/// <summary>
///     Representing speech stored on audio cassettes.
///     Requires a little housekeeping to ensure data on the tape is contiguous,
///     but boils down to data having start/end times, speaker info, and message info.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial struct CassetteTapeAudioInfo
{
    [DataField("spokenMessage")]
    public string SpokenMessage;

    [DataField("speaker")]
    public string Speaker;

    [DataField("startTime")]
    public float StartTime;

    [DataField("entryLength")]
    public float EntryLength;
}
