namespace Content.Shared.Teleportation;

// TODO: When an effect needs the original departure or arrival point, pass captured source and destination
// coordinates in BeforeTeleportEvent and TargetTeleportedEvent instead of reading the target's current transform.

/// <summary>
/// Raised on the teleporter after use checks pass, before the teleport attempt.
/// Subscribers can use it for departure effects. Only raised when effects are enabled.
/// The event has no cancellation flag, but a handler can still invalidate the target or destination
/// and prevent teleportation.
/// </summary>
/// <param name="Target">The entity being teleported.</param>
[ByRefEvent, Serializable]
public readonly record struct BeforeTeleportEvent(EntityUid Target);

/// <summary>
/// Raised on the teleporter after a successful teleport and the target's <see cref="TeleportedEvent"/> notification.
/// Subscribers can use it for arrival effects. Only raised when effects are enabled.
/// Cannot cancel teleportation. Skipped if the target or teleporter is terminating or deleted after notification.
/// </summary>
/// <param name="Target">The entity that was teleported.</param>
[ByRefEvent, Serializable]
public readonly record struct TargetTeleportedEvent(EntityUid Target);

/// <summary>
/// Notifies the teleported entity, even when effects are disabled.
/// Raised before <see cref="TargetTeleportedEvent"/> so a later effect cannot prevent this notification.
/// </summary>
/// <param name="Teleporter">The entity that performed the teleportation.</param>
[ByRefEvent, Serializable]
public readonly record struct TeleportedEvent(EntityUid Teleporter);
