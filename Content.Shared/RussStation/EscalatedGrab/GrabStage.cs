using Robust.Shared.Serialization;

namespace Content.Shared.RussStation.EscalatedGrab;

/// <summary>
/// Escalation level of a grab. Re-clicking pull advances to the next stage.
/// </summary>
[Serializable, NetSerializable]
public enum GrabStage : byte
{
    /// <summary>
    /// Standard pull. Returned by GetStage when no escalation is active
    /// (no <see cref="Components.GrabStateComponent"/> on the puller).
    /// </summary>
    Pull = 0,

    /// <summary>
    /// Aggressive grab. First escalation from a standard pull.
    /// </summary>
    Aggressive = 1,
}
