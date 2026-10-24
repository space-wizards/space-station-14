using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Robust.Shared.Map;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
/// Handles <see cref="TeleportActionEvent"/> for teleports to selected coordinates.
/// </summary>
public sealed partial class TeleportActionSystem : EntitySystem
{
    [Dependency] private SharedTeleportSystem _teleport = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnTeleportAction(TeleportActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryTeleport(
            args.Performer,
            args.Target,
            args.StopBeingPulled,
            args.StopPulling);
    }

    /// <summary>
    /// Checks line of sight and destination clearance before teleporting the entity, optionally stopping its pulls.
    /// </summary>
    /// <param name="target">The entity to teleport.</param>
    /// <param name="destination">The destination coordinates selected by the action.</param>
    /// <param name="stopBeingPulled">Whether to stop another entity from pulling <paramref name="target"/>.</param>
    /// <param name="stopPulling">Whether to stop <paramref name="target"/> from pulling another entity.</param>
    /// <returns>Whether validation passed and the entity was teleported.</returns>
    public bool TryTeleport(
        EntityUid target,
        EntityCoordinates destination,
        bool stopBeingPulled = false,
        bool stopPulling = false)
    {
        if (!destination.IsValid(EntityManager))
            return false;

        var targetXform = Transform(target);
        var destinationXform = Transform(destination.EntityId);
        if (!_examine.InRangeUnOccluded(target, destination, SharedInteractionSystem.MaxRaycastRange))
        {
            _popup.PopupEntity(Loc.GetString("teleport-action-popup-cant-see"), target, target);
            return false;
        }

        var destinationPosRot = _transform.GetWorldPositionRotation(destinationXform);
        var rotatedOffset = destinationPosRot.WorldRotation.RotateVec(destination.Position);
        var destinationMapCoordinates = new MapCoordinates(
            destinationPosRot.WorldPosition + rotatedOffset,
            destinationXform.MapID);

        if (_teleport.IsDestinationBlocked(target, destinationMapCoordinates, destinationPosRot.WorldRotation))
        {
            _popup.PopupEntity(Loc.GetString("teleport-action-popup-blocked"), target, target);
            return false;
        }

        if (stopBeingPulled &&
            TryComp<PullableComponent>(target, out var pullable) &&
            _pulling.IsPulled(target, pullable))
        {
            _pulling.TryStopPull(target, pullable);
        }

        if (stopPulling &&
            TryComp<PullerComponent>(target, out var puller) &&
            TryComp<PullableComponent>(puller.Pulling, out var pulled))
        {
            _pulling.TryStopPull(puller.Pulling.Value, pulled);
        }

        var resolvedDestination = _map.TryFindGridAt(destinationMapCoordinates, out var grid, out _)
            ? _map.MapToGrid(grid, destinationMapCoordinates)
            : _transform.ToCoordinates(destinationMapCoordinates);

        _transform.SetCoordinates(target, targetXform, resolvedDestination);
        return true;
    }
}
