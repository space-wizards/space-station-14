namespace Content.Shared.Teleportation.Triggers;

/// <summary>
/// Passes a target and the user that activated the teleporter to a teleport implementation.
/// Raised on the teleporter entity.
/// Triggers must use SharedTeleportSystem.RequestTeleport to run use checks and prevent nested requests.
/// </summary>
/// <param name="Target">The entity to teleport.</param>
/// <param name="User">The entity that explicitly activated the teleporter, or null for automatic activation.</param>
/// <param name="TriggerEffects">Whether to run effects before and after movement. Does not bypass use checks.</param>
[ByRefEvent, Serializable]
public record struct TeleportRequestEvent(EntityUid Target, EntityUid? User, bool TriggerEffects = true)
{
    /// <summary>
    /// Whether a teleport implementation accepted the request, even if teleportation failed.
    /// Set before execution to prevent another implementation from processing the same request.
    /// </summary>
    public bool Handled;

    /// <summary>
    /// Whether the implementation successfully completed the teleportation.
    /// </summary>
    public bool Succeeded;
}

/// <summary>
/// Checks whether a teleporter can be used by the given target and user.
/// Raised on the teleporter entity.
/// Handlers must only update the cancellation state because this event can be raised while verbs are being collected.
/// </summary>
/// <param name="Target">The entity that would be teleported.</param>
/// <param name="User">The entity that explicitly activates the teleporter, or null for automatic activation.</param>
/// <param name="Cancelled">Whether using the teleporter has been prevented.</param>
/// <param name="CancelReason">Localization key explaining why the teleport is unavailable.</param>
[ByRefEvent, Serializable]
public record struct TeleportUseAttemptEvent(
    EntityUid Target,
    EntityUid? User,
    bool Cancelled = false,
    LocId? CancelReason = null)
{
    /// <summary>
    /// Cancels the attempt, preserving an explanation already provided by another handler.
    /// </summary>
    public void Cancel(LocId? reason = null)
    {
        Cancelled = true;
        CancelReason ??= reason;
    }
}

/// <summary>
/// Notifies a teleporter that one eligible fixture contact with a target has ended.
/// The target may still overlap the trigger; handlers must check its bounds before treating this as an exit.
/// Raised on the teleporter entity.
/// </summary>
/// <param name="Target">The entity whose fixture contact ended.</param>
[ByRefEvent, Serializable]
public readonly record struct TeleportTriggerExitedEvent(EntityUid Target);
