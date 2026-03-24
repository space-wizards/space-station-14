using Robust.Shared.Serialization;

namespace Content.Shared.EscalatedGrab;

/// <summary>
/// Escalation level of a grab. Re-clicking pull advances to the next stage.
/// </summary>
[Serializable, NetSerializable]
public enum GrabStage : byte
{
    /// <summary>
    /// Standard pull. No <see cref="Components.GrabStateComponent"/> exists at this stage.
    /// </summary>
    Pull = 0,

    /// <summary>
    /// Aggressive grab. First escalation from a standard pull.
    /// </summary>
    Aggressive = 1,
}
