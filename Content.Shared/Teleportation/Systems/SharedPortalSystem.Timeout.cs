using Content.Shared.Teleportation.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Teleportation.Systems;

public abstract partial class SharedPortalSystem
{
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
    /// Returns false when this exit cannot trigger for the target.
    /// </summary>
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

    private bool IsTimeoutActive(EntityUid target, PortalTimeoutComponent timeout)
    {
        // An exit outside client visibility is not evidence that the server deleted it.
        if (IsExitUnavailableOnClient(timeout.ExitPortal))
            return true;

        if (!HasComp<PortalComponent>(timeout.ExitPortal))
            return false;

        return _collisionTrigger.IsInsideTriggerBounds(timeout.ExitPortal, target);
    }

    private bool IsExitUnavailableOnClient(EntityUid exit)
    {
        if (!_net.IsClient)
            return false;

        if (!TryComp(exit, out TransformComponent? transform))
            return true;

        return transform.MapID == MapId.Nullspace;
    }

    private void RefreshTimeout(EntityUid target, PortalTimeoutComponent timeout)
    {
        // Movement can synchronously end the source contact before arrival is complete.
        if (HasComp<TeleportingComponent>(target))
            return;

        if (IsTimeoutActive(target, timeout))
            return;

        // Do not queue removal: another request may replace the timeout in the same tick.
        RemComp<PortalTimeoutComponent>(target);
    }

    private void RestoreTimeoutAfterFailedTeleport(EntityUid target, EntityUid? previousExit, bool moved)
    {
        // A failed post-move effect must not remove protection at the exit.
        if (moved)
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
