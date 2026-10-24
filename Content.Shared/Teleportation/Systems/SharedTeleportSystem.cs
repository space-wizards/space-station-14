using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Weapons.Misc;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
/// Handles teleport requests, effects, and target notifications.
/// Teleport implementations supply the destination coordinates.
/// </summary>
public sealed partial class SharedTeleportSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedJointSystem _joints = default!;

    /// <summary>
    /// Attempts to teleport a target, with optional effects on the teleporter.
    /// After a successful teleport, raises <see cref="TeleportedEvent"/> on the target before post-teleport effects.
    /// Used by teleport implementations called through <see cref="RequestTeleport"/>.
    /// </summary>
    /// <remarks>Exceptions from teleportation and its event handlers propagate to the caller.</remarks>
    /// <param name="teleporter">The entity performing the teleportation.</param>
    /// <param name="target">The entity to teleport.</param>
    /// <param name="destination">The destination chosen by the teleport implementation.</param>
    /// <param name="reachedDestination">
    /// Whether the target's transform matches the destination after the coordinate update attempt,
    /// even if a handler throws.
    /// </param>
    /// <param name="triggerEffects">Whether to raise events for effects before and after teleportation.</param>
    /// <returns>Whether teleportation succeeded.</returns>
    internal bool TryTeleport(
        EntityUid teleporter,
        EntityUid target,
        EntityCoordinates destination,
        out bool reachedDestination,
        bool triggerEffects = true)
    {
        reachedDestination = false;

        if (!Exists(teleporter))
            return false;

        if (!CanTeleport(target, destination))
            return false;

        if (triggerEffects)
        {
            var beforeTeleport = new BeforeTeleportEvent(target);
            RaiseLocalEvent(teleporter, ref beforeTeleport);
        }

        // Effects may invalidate the target or destination without issuing another teleport request.
        if (!CanTeleport(target, destination))
            return false;

        try
        {
            Teleport(target, destination);
        }
        finally
        {
            // SetCoordinates can invoke MoveEvent handlers after changing the transform.
            // Preserve whether the destination was reached even if a handler throws.
            reachedDestination = TryComp(target, out TransformComponent? transform) &&
                                 transform.ParentUid == destination.EntityId &&
                                 transform.LocalPosition.EqualsApprox(destination.Position);
        }

        if (!reachedDestination)
            return false;

        var teleported = new TeleportedEvent(teleporter);
        RaiseLocalEvent(target, ref teleported);

        if (!triggerEffects)
            return true;

        // Notification handlers may delete either participant after teleportation succeeds.
        if (TerminatingOrDeleted(target))
            return true;

        if (TerminatingOrDeleted(teleporter))
            return true;

        var targetTeleported = new TargetTeleportedEvent(target);
        RaiseLocalEvent(teleporter, ref targetTeleported);

        return true;
    }

    private bool CanTeleport(EntityUid target, EntityCoordinates destination)
    {
        if (TerminatingOrDeleted(target))
            return false;

        return destination.IsValid(EntityManager);
    }

    private void Teleport(EntityUid target, EntityCoordinates destination)
    {
        StopSpatialRelationships(target);
        _transform.SetCoordinates(target, Transform(target), destination);
    }

    /// <summary>
    /// Detaches pulling and grappling links that cannot safely span the teleport.
    /// </summary>
    private void StopSpatialRelationships(EntityUid target)
    {
        StopPullingRelationships(target);

        // Do not leave a relayed physics joint spanning unrelated coordinates or maps after teleportation.
        _joints.RemoveJoint(target, SharedGrapplingGunSystem.GrapplingJoint);
    }

    /// <summary>
    /// Attempts to stop the target from pulling another entity and from being pulled.
    /// </summary>
    private void StopPullingRelationships(EntityUid target)
    {
        if (TryComp(target, out PullableComponent? targetPullable))
            _pulling.TryStopPull(target, targetPullable);

        if (!TryComp(target, out PullerComponent? targetPuller))
            return;

        var pulledTarget = targetPuller.Pulling;
        if (pulledTarget == null)
            return;

        if (!TryComp(pulledTarget, out PullableComponent? pulledEntity))
            return;

        _pulling.TryStopPull(pulledTarget.Value, pulledEntity);
    }
}
