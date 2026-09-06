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
            // Remote targets are reconciled by the server; only predict our simulated bodies.
            if (_net.IsClient && (!TryComp<PhysicsComponent>(target, out var body) || !body.Predict))
                continue;

            RefreshTimeout(target, timeout);
        }
    }

    /// <summary>
    /// Protects a target from portal use until it leaves the specified exit's trigger bounds.
    /// Returns false when this exit cannot trigger for the target.
    /// </summary>
    public bool SetPortalTimeout(EntityUid target, EntityUid exit)
    {
        if (!HasComp<PortalComponent>(exit) || !_collisionTrigger.CanTrigger(exit, target))
            return false;

        var timeout = EnsureComp<PortalTimeoutComponent>(target);
        timeout.ExitPortal = exit;
        Dirty(target, timeout);
        return true;
    }

    private bool IsTimeoutActive(EntityUid target, PortalTimeoutComponent timeout)
    {
        // An exit outside client visibility is not evidence that the server deleted it.
        if (_net.IsClient && (!TryComp(timeout.ExitPortal, out TransformComponent? exitTransform) ||
                              exitTransform.MapID == MapId.Nullspace))
            return true;

        return HasComp<PortalComponent>(timeout.ExitPortal) &&
               _collisionTrigger.IsInsideTriggerBounds(timeout.ExitPortal, target);
    }

    private void RefreshTimeout(EntityUid target, PortalTimeoutComponent timeout)
    {
        // Movement can synchronously end the source contact before arrival is complete.
        if (HasComp<TeleportingComponent>(target) || IsTimeoutActive(target, timeout))
            return;

        // Do not queue removal: another request may replace the timeout in the same tick.
        RemComp<PortalTimeoutComponent>(target);
    }

    private void RestoreTimeout(EntityUid target, EntityUid? previousExit)
    {
        if (previousExit is not { } exit)
        {
            RemComp<PortalTimeoutComponent>(target);
            return;
        }

        var timeout = EnsureComp<PortalTimeoutComponent>(target);
        timeout.ExitPortal = exit;
        Dirty(target, timeout);
    }
}
