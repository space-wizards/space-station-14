using System.Linq;
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
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityQuery<FixturesComponent> _fixturesQuery;

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

    public bool TryTeleport(
        EntityUid user,
        EntityCoordinates target,
        bool stopBeingPulled = false,
        bool stopPulling = false)
    {
        if (!target.IsValid(EntityManager))
            return false;

        var xform = Transform(user);
        var mapTarget = _transform.ToMapCoordinates(target);
        if (xform.MapID != mapTarget.MapId ||
            !_examine.InRangeUnOccluded(user, target, SharedInteractionSystem.MaxRaycastRange))
        {
            _popup.PopupEntity(Loc.GetString("teleport-action-popup-cant-see"), user, user);
            return false;
        }

        if (IsDestinationBlocked(user, mapTarget, xform))
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

        var destination = _map.TryFindGridAt(mapTarget, out var grid, out _)
            ? _map.MapToGrid(grid, mapTarget)
            : _transform.ToCoordinates(mapTarget);

        _transform.SetCoordinates(user, xform, destination);
        return true;
    }

    public bool IsDestinationBlocked(
        EntityUid user,
        MapCoordinates target,
        TransformComponent? xform = null,
        FixturesComponent? fixtures = null,
        PhysicsComponent? physics = null)
    {
        if (!Resolve(user, ref xform, ref fixtures, ref physics, false) ||
            !physics.CanCollide ||
            !physics.Hard)
        {
            return false;
        }

        var destinationTransform = new Transform(
            target.Position,
            _transform.GetWorldRotation(xform));

        var intersecting = new HashSet<Entity<PhysicsComponent>>();

        foreach (var fixture in fixtures.Fixtures.Values.Where(fixture => fixture.Hard))
        {
            intersecting.Clear();
            _lookup.GetEntitiesIntersecting(
                target.MapId,
                fixture.Shape,
                destinationTransform,
                intersecting,
                LookupFlags.Dynamic | LookupFlags.Static);

            foreach (var other in intersecting)
            {
                if (other.Owner == user ||
                    !other.Comp.CanCollide ||
                    !other.Comp.Hard ||
                    !_fixturesQuery.TryComp(other, out var otherFixtures))
                {
                    continue;
                }

                var (layer, mask) = SharedPhysicsSystem.GetHardCollision(otherFixtures);
                if ((fixture.CollisionMask & layer) != 0 ||
                    (mask & fixture.CollisionLayer) != 0)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
