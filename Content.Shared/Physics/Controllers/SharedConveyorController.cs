using System.Numerics;
using Content.Shared.Conveyor;
using Content.Shared.Gravity;
using Content.Shared.Movement.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Physics.Controllers;

public abstract class SharedConveyorController : VirtualController
{
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] protected readonly EntityLookupSystem Lookup = default!;
    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

    protected const string ConveyorFixture = "conveyor";

    private static readonly Vector2 _expansion = new Vector2(0.1f, 0.1f);

    public override void Initialize()
    {
        UpdatesAfter.Add(typeof(SharedMoverController));

        SubscribeLocalEvent<ConveyorComponent, StartCollideEvent>(OnConveyorStartCollide);
        SubscribeLocalEvent<ConveyorComponent, EndCollideEvent>(OnConveyorEndCollide);

        base.Initialize();
    }

    private void OnConveyorStartCollide(EntityUid uid, ConveyorComponent component, ref StartCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        if (args.OtherBody.BodyType == BodyType.Static || component.State == ConveyorState.Off)
            return;

        component.Intersecting.Add(otherUid);
        EnsureComp<ActiveConveyorComponent>(uid);
    }

    private void OnConveyorEndCollide(EntityUid uid, ConveyorComponent component, ref EndCollideEvent args)
    {
        component.Intersecting.Remove(args.OtherEntity);

        if (component.Intersecting.Count == 0)
            RemComp<ActiveConveyorComponent>(uid);
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        var conveyed = new HashSet<EntityUid>();
        // Don't use it directly in EntityQuery because we may be able to save getcomponents.
        var xformQuery = GetEntityQuery<TransformComponent>();
        var bodyQuery = GetEntityQuery<PhysicsComponent>();
        var query = EntityQueryEnumerator<ActiveConveyorComponent, ConveyorComponent>();

        while (query.MoveNext(out var uid, out var _, out var comp))
        {
            Convey(uid, comp, xformQuery, bodyQuery, conveyed, frameTime, prediction);
        }
    }

    private void Convey(EntityUid uid, ConveyorComponent comp, EntityQuery<TransformComponent> xformQuery, EntityQuery<PhysicsComponent> bodyQuery, HashSet<EntityUid> conveyed, float frameTime, bool prediction)
    {
        // Use an event for conveyors to know what needs to run
        if (!CanRun(comp))
            return;

        var speed = comp.Speed;

        if (speed <= 0f || !xformQuery.TryGetComponent(uid, out var xform) || xform.GridUid == null)
            return;

        var conveyorPos = xform.LocalPosition;
        var conveyorRot = xform.LocalRotation;

        conveyorRot += comp.Angle;

        if (comp.State == ConveyorState.Reverse)
            conveyorRot += MathF.PI;

        var direction = conveyorRot.ToWorldVec();

        foreach (var (entity, transform, body) in GetEntitiesToMove(comp, xform, xformQuery, bodyQuery))
        {
            if (!conveyed.Add(entity) || prediction && !body.Predict)
                continue;

            var localPos = transform.LocalPosition;
            var itemRelative = conveyorPos - localPos;

            localPos += Convey(direction, speed, frameTime, itemRelative);
            transform.LocalPosition = localPos;

            // Force it awake for collisionwake reasons.
            Physics.SetAwake(entity, body, true);
            Physics.SetSleepTime(body, 0f);
        }
        Dirty(comp);
    }

    private static Vector2 Convey(Vector2 direction, float speed, float frameTime, Vector2 itemRelative)
    {
        if (speed == 0 || direction.Length() == 0)
            return Vector2.Zero;

        /*
         * Basic idea: if the item is not in the middle of the conveyor in the direction that the conveyor is running,
         * move the item towards the middle. Otherwise, move the item along the direction. This lets conveyors pick up
         * items that are not perfectly aligned in the middle, and also makes corner cuts work.
         *
         * We do this by computing the projection of 'itemRelative' on 'direction', yielding a vector 'p' in the direction
         * of 'direction'. We also compute the rejection 'r'. If the magnitude of 'r' is not (near) zero, then the item
         * is not on the centerline.
         */

        var p = direction * (Vector2.Dot(itemRelative, direction) / Vector2.Dot(direction, direction));
        var r = itemRelative - p;

        if (r.Length() < 0.1)
        {
            var velocity = direction * speed;
            return velocity * frameTime;
        }
        else
        {
            // Give a slight nudge in the direction of the conveyor to prevent
            // to collidable objects (e.g. crates) on the locker from getting stuck
            // pushing each other when rounding a corner.
            var velocity = (r + direction*0.2f).Normalized() * speed;
            return velocity * frameTime;
        }
    }

    private IEnumerable<(EntityUid, TransformComponent, PhysicsComponent)> GetEntitiesToMove(
        ConveyorComponent comp,
        TransformComponent xform,
        EntityQuery<TransformComponent> xformQuery,
        EntityQuery<PhysicsComponent> bodyQuery)
    {
        // Check if the thing's centre overlaps the grid tile.
        var grid = MapManager.GetGrid(xform.GridUid!.Value);
        var tile = grid.GetTileRef(xform.Coordinates);
        var conveyorBounds = Lookup.GetLocalBounds(tile, grid.TileSize);

        foreach (var entity in comp.Intersecting)
        {
            if (!xformQuery.TryGetComponent(entity, out var entityXform) || entityXform.ParentUid != grid.Owner)
                continue;

            if (!bodyQuery.TryGetComponent(entity, out var physics) || physics.BodyType == BodyType.Static || physics.BodyStatus == BodyStatus.InAir || _gravity.IsWeightless(entity, physics, entityXform))
                continue;

            // Yes there's still going to be the occasional rounding issue where it stops getting conveyed
            // When you fix the corner issue that will fix this anyway.
            var gridAABB = new Box2(entityXform.LocalPosition - _expansion, entityXform.LocalPosition + _expansion);

            if (!conveyorBounds.Intersects(gridAABB))
                continue;

            yield return (entity, entityXform, physics);
        }
    }

    public bool CanRun(ConveyorComponent component)
    {
        return component.State != ConveyorState.Off && component.Powered;
    }
}
