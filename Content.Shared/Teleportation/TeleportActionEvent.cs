using Content.Shared.Actions;

namespace Content.Shared.Teleportation;

/// <summary>
/// Requests that the performer be teleported to the targeted world position.
/// </summary>
public sealed partial class TeleportActionEvent : WorldTargetActionEvent
{
    /// <summary>
    /// Whether to stop the performer from being pulled when the teleport succeeds.
    /// </summary>
    [DataField]
    public bool StopBeingPulled;

    /// <summary>
    /// Whether to make the performer stop pulling another entity when the teleport succeeds.
    /// </summary>
    [DataField]
    public bool StopPulling;
}
