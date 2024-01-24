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
    /// Networked for client side prediction
    /// </summary>
    [DataField]
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
}
