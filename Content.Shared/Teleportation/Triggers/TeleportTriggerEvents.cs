namespace Content.Shared.Teleportation.Triggers;

/// <summary>
/// Requests teleportation of a target. Raised on the teleporter entity.
/// Triggers must call SharedTeleportSystem.RequestTeleport so use checks run and nested requests are blocked.
/// </summary>
/// <param name="Target">The entity to teleport.</param>
/// <param name="User">The entity that explicitly activated the teleporter, or null for automatic activation.</param>
/// <param name="TriggerEffects">
/// Whether to run effects before and after teleportation. Does not bypass use checks.
/// </param>
[ByRefEvent, Serializable]
public record struct TeleportRequestEvent(EntityUid Target, EntityUid? User, bool TriggerEffects = true)
{
    /// <summary>
    /// Whether an implementation has accepted this request. Teleportation may still fail.
    /// Set before teleporting so another implementation cannot handle the same request.
    /// </summary>
    public bool Handled;

    /// <summary>
    /// Whether the implementation completed teleportation successfully.
    /// </summary>
    public bool Succeeded;
}

/// <summary>
/// Checks whether a teleporter can be used by the given target and user.
/// Raised on the teleporter entity.
/// Also raised when building verb menus. Handlers must only change the cancellation state.
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
    /// Cancels the attempt without replacing a reason already provided by another handler.
    /// </summary>
    /// <param name="reason">
    /// Optional localization key. An existing cancellation reason is kept.
    /// </param>
    public void Cancel(LocId? reason = null)
    {
        Cancelled = true;
        CancelReason ??= reason;
    }
}

/// <summary>
/// Raised on the teleporter when a matching fixture contact with the target ends.
/// The target may still overlap the trigger. Handlers must check its bounds before treating this as an exit.
/// </summary>
/// <param name="Target">The entity whose fixture contact ended.</param>
[ByRefEvent, Serializable]
public readonly record struct TeleportTriggerExitedEvent(EntityUid Target);
