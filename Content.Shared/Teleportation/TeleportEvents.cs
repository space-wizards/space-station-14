using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation;

// TODO: When an effect needs the original departure or arrival point, pass captured source and destination
// coordinates in BeforeTeleportEvent and TargetTeleportedEvent instead of reading the target's current transform.

/// <summary>
/// Triggers effects immediately before a teleporter moves a target, after use checks have passed.
/// Raised on the teleporter entity only when effects are enabled. Cannot cancel teleportation.
/// </summary>
/// <param name="Target">The entity being teleported.</param>
[ByRefEvent, Serializable]
public readonly record struct BeforeTeleportEvent(EntityUid Target);

/// <summary>
/// Triggers effects after a teleporter has moved a target and notified it with <see cref="TeleportedEvent"/>.
/// Raised on the teleporter entity only when effects are enabled. Cannot cancel teleportation.
/// </summary>
/// <param name="Target">The entity that was teleported.</param>
[ByRefEvent, Serializable]
public readonly record struct TargetTeleportedEvent(EntityUid Target);

/// <summary>
/// Notifies a target that it has been teleported.
/// Raised on the teleported entity even when effects are disabled.
/// Raised before <see cref="TargetTeleportedEvent"/>, so a post-move effect cannot prevent this notification.
/// </summary>
/// <param name="Teleporter">The entity that performed the teleportation.</param>
[ByRefEvent, Serializable]
public readonly record struct TeleportedEvent(EntityUid Teleporter);
