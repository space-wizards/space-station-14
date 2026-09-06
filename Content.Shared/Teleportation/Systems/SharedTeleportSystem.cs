using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Weapons.Misc;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
/// Moves entities to resolved destination coordinates, with optional effects before and after movement.
/// </summary>
public sealed partial class SharedTeleportSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedJointSystem _joints = default!;

    /// <summary>
    /// Attempts to move a target, optionally triggering effects on the teleporter.
    /// Notifies the target with <see cref="TeleportedEvent"/> after successful movement, before post-move effects.
    /// Used by teleport implementations while handling a request dispatched by <see cref="RequestTeleport"/>.
    /// </summary>
    /// <param name="teleporter">The entity performing the teleportation.</param>
    /// <param name="target">The entity to teleport.</param>
    /// <param name="destination">The destination chosen by the teleport implementation.</param>
    /// <param name="moved">Whether the target reached the destination, even if a later effect throws.</param>
    /// <param name="triggerEffects">Whether to raise the pre-move and post-move effect events.</param>
    internal bool TryTeleport(EntityUid teleporter, EntityUid target, EntityCoordinates destination, out bool moved, bool triggerEffects = true)
    {
        moved = false;

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
            // SetCoordinates can raise movement handlers after changing the transform.
            // Preserve the movement result even if one of those handlers throws.
            moved = TryComp(target, out TransformComponent? transform) &&
                    transform.ParentUid == destination.EntityId &&
                    transform.LocalPosition.EqualsApprox(destination.Position);
        }

        if (!moved)
            return false;

        var teleported = new TeleportedEvent(teleporter);
        RaiseLocalEvent(target, ref teleported);

        if (!triggerEffects)
            return true;

        // Notification handlers may delete either participant after the movement succeeded.
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
        if (!Exists(target))
            return false;

        if (TerminatingOrDeleted(target))
            return false;

        return destination.IsValid(EntityManager);
    }

    private void Teleport(EntityUid target, EntityCoordinates destination)
    {
        StopSpatialRelationships(target);
        _transform.SetCoordinates(target, Transform(target), destination);
    }

    private void StopSpatialRelationships(EntityUid target)
    {
        StopPullingRelationships(target);

        // Do not leave a relayed physics joint spanning unrelated coordinates or maps after teleportation.
        _joints.RemoveJoint(target, SharedGrapplingGunSystem.GrapplingJoint);
    }

    private void StopPullingRelationships(EntityUid target)
    {
        if (TryComp(target, out PullableComponent? targetPullable))
            _pulling.TryStopPull(target, targetPullable);

        if (!TryComp(target, out PullerComponent? targetPuller))
            return;

        if (targetPuller.Pulling is not { } pulledTarget)
            return;

        if (!TryComp(pulledTarget, out PullableComponent? pulledEntity))
            return;

        _pulling.TryStopPull(pulledTarget, pulledEntity);
    }
}
