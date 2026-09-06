using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation;

/// <summary>
/// Triggers effects immediately before a teleporter moves a target, after use checks have passed.
/// Raised on the teleporter entity only when effects are enabled. Cannot cancel teleportation.
/// </summary>
/// <param name="Target">The entity being teleported.</param>
[ByRefEvent, Serializable]
public record struct BeforeTeleportEvent(EntityUid Target);

/// <summary>
/// Triggers effects after a teleporter has moved a target.
/// Raised on the teleporter entity only when effects are enabled. Cannot cancel teleportation.
/// </summary>
/// <param name="Target">The entity that was teleported.</param>
[ByRefEvent, Serializable]
public record struct TargetTeleportedEvent(EntityUid Target);

/// <summary>
/// Notifies a target that it has been teleported.
/// Raised on the teleported entity even when effects are disabled.
/// The low-level relocation overload without a teleporter does not raise this event.
/// </summary>
/// <param name="Teleporter">The entity that performed the teleportation.</param>
[ByRefEvent, Serializable]
public record struct TeleportedEvent(EntityUid Teleporter);
