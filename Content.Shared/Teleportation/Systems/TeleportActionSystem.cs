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
    /// <param name="user">The entity to teleport.</param>
    /// <param name="target">The destination coordinates selected by the action.</param>
    /// <param name="stopBeingPulled">Whether to stop another entity from pulling <paramref name="user"/>.</param>
    /// <param name="stopPulling">Whether to stop <paramref name="user"/> from pulling another entity.</param>
    /// <returns>Whether validation passed and the entity was teleported.</returns>
    public bool TryTeleport(
        EntityUid user,
        EntityCoordinates target,
        bool stopBeingPulled = false,
        bool stopPulling = false)
    {
        if (!target.IsValid(EntityManager))
            return false;

        var xform = Transform(user);
        var targetXform = Transform(target.EntityId);
        if (!_examine.InRangeUnOccluded(user, target, SharedInteractionSystem.MaxRaycastRange))
        {
            _popup.PopupEntity(Loc.GetString("teleport-action-popup-cant-see"), user, user);
            return false;
        }

        var targetEntPosRot = _transform.GetWorldPositionRotation(targetXform);
        var targetRotated = targetEntPosRot.WorldRotation.RotateVec(target.Position);
        var targetMapCoordinates = new MapCoordinates(targetEntPosRot.WorldPosition + targetRotated, targetXform.MapID);

        if (_teleport.IsDestinationBlocked(user, targetMapCoordinates, targetEntPosRot.WorldRotation))
        {
            _popup.PopupEntity(Loc.GetString("teleport-action-popup-blocked"), user, user);
            return false;
        }

        if (stopBeingPulled &&
            TryComp<PullableComponent>(user, out var pullable) &&
            _pulling.IsPulled(user, pullable))
        {
            _pulling.TryStopPull(user, pullable);
        }

        if (stopPulling &&
            TryComp<PullerComponent>(user, out var puller) &&
            TryComp<PullableComponent>(puller.Pulling, out var pulled))
        {
            _pulling.TryStopPull(puller.Pulling.Value, pulled);
        }

        var destination = _map.TryFindGridAt(targetMapCoordinates, out var grid, out _)
            ? _map.MapToGrid(grid, targetMapCoordinates)
            : _transform.ToCoordinates(targetMapCoordinates);

        _transform.SetCoordinates(user, xform, destination);
        return true;
    }
}
