using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.CassetteTape.Components;

/// <summary>
/// Represents the playhead mechanisms that allow a cassette tape to be recorded to and played-back from.
/// </summary>
[RegisterComponent]
public sealed partial class CassettePlayheadComponent : Component
{
    /// <summary>
    /// The container ID that is holds the tape to be played or recorded.
    /// </summary>
    [DataField("slotId", required: true)]
    public string SlotId = string.Empty;

    /// <summary>
    /// Do we have a tape inserted?
    /// </summary>
    [DataField("hasTape", required: false)]
    public bool HasTape = false;

    /// <summary>
    /// Current state of playhead - Standby, Playing, Recording
    /// </summary>
    [DataField("playheadState", required: false)]
    public CassettePlayheadState PlayheadState = CassettePlayheadState.Standby;

    /// <summary>
    /// Current target active state - Playback or Recording
    /// </summary>
    [DataField("targetActiveState", required: false)]
    public CassettePlayheadState TargetActiveState = CassettePlayheadState.Recording;

    /// <summary>
    /// Current location of playhead, in seconds.
    /// </summary>
    [DataField("playheadLocation", required: false)]
    public float PlayheadLocation = 0.0f;

    /// <summary>
    /// The ID of the entity that owns the currently inserted tape.
    /// </summary>
    [DataField("currentTape", required: false)]
    public CassetteTapeComponent? CurrentTape = null;

    /// <summary>
    /// The sound that should be played when the motor starts.
    /// </summary>
    [DataField("playMotorSound", required: true)]
    public string PlayMotorSound = string.Empty;

    /// <summary>
    /// The sound that should be played when the motor stops, or when no tape is inserted.
    /// </summary>
    [DataField("stopClunkSound", required: true)]
    public string StopClunkSound = string.Empty;

    [DataField]
    public EntProtoId ToggleAction = "ActionToggleCassetteRecorder";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;
}


[Serializable]
public enum CassettePlayheadState : byte
{
    Standby,
    Recording,
    Playing
}
