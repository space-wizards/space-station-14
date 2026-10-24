using Content.Shared.Teleportation.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Teleportation.Systems;

public abstract partial class SharedPortalSystem
{
    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PortalTimeoutComponent>();
        while (query.MoveNext(out var target, out var timeout))
        {
            if (!ShouldUpdateTimeout(target))
                continue;

            RefreshTimeout(target, timeout);
        }
    }

    /// <summary>
    /// The server refreshes all timeouts. Clients refresh timeouts only for physics-predicted targets.
    /// </summary>
    private bool ShouldUpdateTimeout(EntityUid target)
    {
        if (!_net.IsClient)
            return true;

        // On clients, update timeouts only for bodies with physics prediction enabled.
        // Other entities receive timeout changes from the server.
        if (!TryComp<PhysicsComponent>(target, out var body))
            return false;

        return body.Predict;
    }

    /// <summary>
    /// Protects a target from portal use until it leaves the specified exit's trigger bounds.
    /// </summary>
    /// <remarks>
    /// The target does not need to overlap the exit yet.
    /// </remarks>
    /// <param name="target">The entity to protect from further portal use.</param>
    /// <param name="exit">The destination portal.</param>
    /// <returns>
    /// Whether protection was set. Returns false if the exit is not a portal or cannot trigger for the target.
    /// </returns>
    public bool SetPortalTimeout(EntityUid target, EntityUid exit)
    {
        if (!HasComp<PortalComponent>(exit))
            return false;

        if (!_collisionTrigger.CanTrigger(exit, target))
            return false;

        var timeout = EnsureComp<PortalTimeoutComponent>(target);
        timeout.ExitPortal = exit;
        Dirty(target, timeout);
        return true;
    }

    /// <summary>
    /// Retains protection while the target overlaps its exit, or while the client cannot see that exit.
    /// </summary>
    private bool IsTimeoutActive(EntityUid target, PortalTimeoutComponent timeout)
    {
        // An exit outside client visibility is not evidence that the server deleted it.
        if (IsExitUnavailableOnClient(timeout.ExitPortal))
            return true;

        if (!HasComp<PortalComponent>(timeout.ExitPortal))
            return false;

        return _collisionTrigger.IsInsideTriggerBounds(timeout.ExitPortal, target);
    }

    /// <summary>
    /// Checks whether the client lacks the exit's transform or sees it in nullspace.
    /// </summary>
    private bool IsExitUnavailableOnClient(EntityUid exit)
    {
        if (!_net.IsClient)
            return false;

        if (!TryComp(exit, out TransformComponent? transform))
            return true;

        return transform.MapID == MapId.Nullspace;
    }

    /// <summary>
    /// Clears inactive exit protection after teleport processing ends.
    /// </summary>
    private void RefreshTimeout(EntityUid target, PortalTimeoutComponent timeout)
    {
        // Changing coordinates can synchronously end the source contact before arrival is complete.
        if (HasComp<TeleportingComponent>(target))
            return;

        if (IsTimeoutActive(target, timeout))
            return;

        // Do not queue removal: another request may replace the timeout in the same tick.
        RemComp<PortalTimeoutComponent>(target);
    }

    /// <summary>
    /// Restores the previous exit protection if the target did not reach the destination.
    /// Preserves destination protection once the target has reached it.
    /// </summary>
    private void RestoreTimeoutAfterFailedTeleport(EntityUid target, EntityUid? previousExit, bool reachedDestination)
    {
        // A failed effect after teleportation must not remove protection at the exit.
        if (reachedDestination)
            return;

        if (TerminatingOrDeleted(target))
            return;

        if (previousExit == null)
        {
            RemComp<PortalTimeoutComponent>(target);
            return;
        }

        var timeout = EnsureComp<PortalTimeoutComponent>(target);
        timeout.ExitPortal = previousExit.Value;
        Dirty(target, timeout);
    }
}
