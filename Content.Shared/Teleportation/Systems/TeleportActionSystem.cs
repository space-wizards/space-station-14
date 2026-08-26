using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
/// Handles <see cref="TeleportActionEvent"/> by checking line of sight and whether the performer
/// would collide with anything at the destination before moving them.
/// </summary>
public sealed partial class TeleportActionSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<PhysicsComponent>> _intersecting = new();

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
    /// Attempts to teleport user entity to target coordinates. Stops pulling or being pulled if requested.
    /// </summary>
    /// <param name="user">Entity to teleport.</param>
    /// <param name="target">Target coordinates.</param>
    /// <param name="stopBeingPulled">Should <see cref="user"/> stop being used in pulling interaction?</param>
    /// <param name="stopPulling">Should <see cref="user"/> stop pulling interaction on any entity he is currently pulling?</param>
    /// <returns></returns>
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
        if (xform.MapID != targetXform.MapID ||
            !_examine.InRangeUnOccluded(user, target, SharedInteractionSystem.MaxRaycastRange))
        {
            _popup.PopupEntity(Loc.GetString("teleport-action-popup-cant-see"), user, user);
            return false;
        }

        var targetEntPosRot = _transform.GetWorldPositionRotation(targetXform);
        var targetRotated = targetEntPosRot.WorldRotation.RotateVec(target.Position);
        var targetMapCoordinates = new MapCoordinates(targetEntPosRot.WorldPosition + targetRotated, targetXform.MapID);

        if (IsDestinationBlocked(user, targetMapCoordinates, targetEntPosRot.WorldRotation))
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

    /// <summary>
    /// Checks whether the entity would collide with another hard, collidable entity at the target coordinates.
    /// </summary>
    /// <param name="user">The entity being checked.</param>
    /// <param name="target">The map coordinates to check.</param>
    /// <param name="rotation">The rotation of the entity at the target coordinates.</param>
    /// <param name="fixtures">The entity's fixtures component.</param>
    /// <param name="physics">The entity's physics component.</param>
    /// <returns>Whether the destination is blocked.</returns>
    public bool IsDestinationBlocked(
        EntityUid user,
        MapCoordinates target,
        Angle rotation,
        FixturesComponent? fixtures = null,
        PhysicsComponent? physics = null
    )
    {
        if (!Resolve(user, ref fixtures, ref physics, false) ||
            !physics.CanCollide ||
            !physics.Hard)
        {
            return false;
        }

        var destinationTransform = new Transform(target.Position, rotation);

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            if (!fixture.Hard)
                continue;

            _intersecting.Clear();
            _lookup.GetEntitiesIntersecting(
                target.MapId,
                fixture.Shape,
                destinationTransform,
                _intersecting,
                LookupFlags.Dynamic | LookupFlags.Static
            );

            foreach (var other in _intersecting)
            {
                if (other.Owner == user)
                    continue;

                if (_physics.IsCurrentlyHardCollidable((other.Owner, null, other.Comp), (user, fixtures, physics)))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
