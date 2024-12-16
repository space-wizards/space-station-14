using System.Numerics;
using Content.Shared.Conveyor;
using Content.Shared.Gravity;
using Content.Shared.Movement.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Events;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Threading;

namespace Content.Shared.Physics.Controllers;

public abstract class SharedConveyorController : VirtualController
{
    /*
     * Conveyors move entities directly rather than using velocity as we don't want to interfere with other sources
     * of velocity.
     */

    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] private   readonly IParallelManager _parallel = default!;
    [Dependency] protected readonly EntityLookupSystem Lookup = default!;
    [Dependency] private   readonly RayCastSystem _ray = default!;
    [Dependency] private   readonly SharedGravitySystem _gravity = default!;
    [Dependency] private   readonly SharedMapSystem _maps = default!;

    protected const string ConveyorFixture = "conveyor";

    private ConveyorJob _job;

    private EntityQuery<MapGridComponent> _gridQuery;
    protected EntityQuery<PhysicsComponent> PhysicsQuery;
    protected EntityQuery<TransformComponent> XformQuery;

    protected HashSet<EntityUid> Intersecting = new();

    private HashSet<Entity<ConveyorComponent>> _conveyors = new();

    private List<Entity<ConveyorBlockerComponent>> _expiredBlockers = new();

    private ValueList<EntityUid> _ents = new();

    /// <summary>
    /// How much a conveyed item needs to move in the desired direction to avoid sleeping.
    /// </summary>
    private const float DirectionAmount = 0.3f;

    public override void Initialize()
    {
        _job = new ConveyorJob(this);
        _gridQuery = GetEntityQuery<MapGridComponent>();
        PhysicsQuery = GetEntityQuery<PhysicsComponent>();
        XformQuery = GetEntityQuery<TransformComponent>();

        UpdatesAfter.Add(typeof(SharedMoverController));

        SubscribeLocalEvent<ConveyedComponent, ComponentStartup>(OnConveyedStartup);
        SubscribeLocalEvent<ConveyedComponent, PhysicsWakeEvent>(OnConveyedWake);
        SubscribeLocalEvent<ConveyedComponent, PhysicsSleepEvent>(OnConveyedSleep);

        SubscribeLocalEvent<ConveyorComponent, StartCollideEvent>(OnConveyorStartCollide);
        SubscribeLocalEvent<ConveyorComponent, EndCollideEvent>(OnConveyorEndCollide);

        SubscribeLocalEvent<ConveyorBlockerComponent, ComponentStartup>(OnBlockerStartup);
        SubscribeLocalEvent<ConveyorBlockerComponent, EntityTerminatingEvent>(OnBlockerShutdown);
        SubscribeLocalEvent<ConveyorBlockerComponent, MoveEvent>(OnBlockerMove);

        base.Initialize();
    }

    private void OnConveyedStartup(Entity<ConveyedComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<ActiveConveyedComponent>(ent);
    }

    private void OnConveyedWake(Entity<ConveyedComponent> ent, ref PhysicsWakeEvent args)
    {
        EnsureComp<ActiveConveyedComponent>(ent);
    }

    private void OnConveyedSleep(Entity<ConveyedComponent> ent, ref PhysicsSleepEvent args)
    {
        RemCompDeferred<ActiveConveyedComponent>(ent);
    }

    private void OnBlockerStartup(Entity<ConveyorBlockerComponent> ent, ref ComponentStartup args)
    {
        // If we serialize one that was shutting down then still handle it.
        if (ent.Comp.Expired)
        {
            _expiredBlockers.Add(ent);
        }
    }

    private void OnBlockerMove(Entity<ConveyorBlockerComponent> ent, ref MoveEvent args)
    {
        ExpireBlocker(ent);
    }

    private void OnBlockerShutdown(Entity<ConveyorBlockerComponent> ent, ref EntityTerminatingEvent args)
    {
        ExpireBlocker(ent);
    }

    private void ExpireBlocker(Entity<ConveyorBlockerComponent> ent)
    {
        var xform = Transform(ent.Owner);

        if (xform.GridUid == null)
            return;

        // Query for conveyors on neighbouring tiles and awaken entities.
        _conveyors.Clear();
        // TODO: Move to physics system
        var bounds = Lookup.GetAABBNoContainer(ent.Owner, xform.LocalPosition, xform.LocalRotation).Enlarged(0.15f);

        Lookup.GetLocalEntitiesIntersecting(xform.GridUid.Value,
            bounds,
            _conveyors,
            flags: LookupFlags.Static | LookupFlags.Sensors);

        foreach (var conveyor in _conveyors)
        {
            if (TerminatingOrDeleted(conveyor.Owner))
                continue;

            AwakenConveyor(conveyor.Owner);
        }

        ent.Comp.Expired = true;
        _expiredBlockers.Add(ent);
    }

    protected virtual void AwakenConveyor(Entity<TransformComponent?> ent)
    {

    }

    private void OnConveyorStartCollide(EntityUid uid, ConveyorComponent component, ref StartCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        if (!args.OtherFixture.Hard || args.OtherBody.BodyType == BodyType.Static || component.State == ConveyorState.Off)
            return;

        var conveyed = EnsureComp<ConveyedComponent>(otherUid);

        if (conveyed.Colliding.Contains(uid))
            return;

        conveyed.Colliding.Add(uid);
        Dirty(otherUid, conveyed);
    }

    private void OnConveyorEndCollide(Entity<ConveyorComponent> ent, ref EndCollideEvent args)
    {
        if (!TryComp(args.OtherEntity, out ConveyedComponent? conveyed))
            return;

        if (!conveyed.Colliding.Remove(ent.Owner))
            return;

        Dirty(args.OtherEntity, conveyed);
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        foreach (var expired in _expiredBlockers)
        {
            RemComp<ConveyorBlockerComponent>(expired);
        }

        var blockerQuery = EntityQueryEnumerator<ConveyorBlockerComponent>();

        _expiredBlockers.Clear();

        _job.FrameTime = frameTime;
        _job.Prediction = prediction;
        _job.Conveyed.Clear();
        _ents.Clear();

        var query = EntityQueryEnumerator<ConveyedComponent, FixturesComponent, PhysicsComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var fixtures, out var body, out var xform))
        {
            _job.Conveyed.Add(((uid, comp, fixtures, body, xform), Vector2.Zero, false));
        }

        for (var i = _job.Conveyors.Count; i < _job.Conveyed.Count; i++)
        {
            _job.Conveyors.Add(new HashSet<Entity<ConveyorComponent>>());
        }

        _parallel.ProcessNow(_job, _job.Conveyed.Count);

        foreach (var ent in _job.Conveyed)
        {
            if (!ent.Result)
            {
                _ents.Add(ent.Entity.Owner);
                continue;
            }

            // Not conveying anywhere.
            if (ent.Direction.Equals(Vector2.Zero))
            {
                continue;
            }

            var physics = ent.Entity.Comp3;
            var xform = ent.Entity.Comp4;

            // Force it awake for collisionwake reasons.
            PhysicsSystem.SetAwake((ent.Entity.Owner, physics), true);
            PhysicsSystem.SetSleepTime(physics, 0f);

            TransformSystem.SetLocalPosition(ent.Entity.Owner, xform.LocalPosition + ent.Direction, xform);
        }

        foreach (var ent in _ents)
        {
            RemComp<ConveyedComponent>(ent);
        }
    }

    private bool TryConvey(Entity<ConveyedComponent, FixturesComponent, PhysicsComponent, TransformComponent> entity,
        HashSet<Entity<ConveyorComponent>> conveyors,
        bool prediction,
        float frameTime,
        out Vector2 direction)
    {
        direction = Vector2.Zero;
        var fixtures = entity.Comp2;
        var physics = entity.Comp3;
        var xform = entity.Comp4;
        var contacting = entity.Comp1.Colliding.Count > 0;

        if (!contacting)
            return false;

        // Client moment
        if (!physics.Predict && prediction)
            return true;

        if (physics.BodyType == BodyType.Static)
            return false;

        if (!_gridQuery.TryComp(xform.GridUid, out var grid))
            return true;

        var gridTile = _maps.TileIndicesFor(xform.GridUid.Value, grid, xform.Coordinates);

        // Check for any conveyors on the attached tile.
        // If your conveyor doesn't work at all then you may need Static here as a flag
        Lookup.GetLocalEntitiesIntersecting(xform.GridUid.Value, gridTile, conveyors, gridComp: grid,
            flags: LookupFlags.Static | LookupFlags.Sensors | LookupFlags.Approximate);

        // No more conveyors.
        if (conveyors.Count == 0)
        {
            return false;
        }

        if (physics.BodyStatus == BodyStatus.InAir ||
            _gravity.IsWeightless(entity, physics, xform))
        {
            return true;
        }

        Entity<ConveyorComponent> bestConveyor = default;
        var bestSpeed = 0f;

        foreach (var conveyor in conveyors)
        {
            if (conveyor.Comp.Speed > bestSpeed && CanRun(conveyor))
            {
                bestSpeed = conveyor.Comp.Speed;
                bestConveyor = conveyor;
            }
        }

        if (bestSpeed == 0f || bestConveyor == default)
            return true;

        var comp = bestConveyor.Comp!;
        var conveyorXform = XformQuery.GetComponent(bestConveyor.Owner);
        var conveyorPos = conveyorXform.LocalPosition;
        var conveyorRot = conveyorXform.LocalRotation;

        conveyorRot += bestConveyor.Comp!.Angle;

        if (comp.State == ConveyorState.Reverse)
            conveyorRot += MathF.PI;

        var conveyorDirection = conveyorRot.ToWorldVec();
        direction = conveyorDirection;

        var itemRelative = conveyorPos - xform.LocalPosition;

        direction = Convey(direction, bestSpeed, frameTime, itemRelative);

        // Shapecast to the desired spot
        var result = new RayResult();

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            if (!fixture.Hard)
                continue;

            var filter = new QueryFilter
            {
                LayerBits = fixture.CollisionLayer,
                MaskBits = fixture.CollisionMask,
                Flags = QueryFlags.Static,
            };

            var transform = PhysicsSystem.GetLocalPhysicsTransform(entity.Owner, xform);
            transform.Position += direction;

            foreach (var contact in fixture.Contacts.Values)
            {
                var normal = contact.Manifold.LocalNormal;

                // If it's overlapping already and we're not conveying in that direction.
                if (contact.IsTouching && contact.Hard && Vector2.Dot(conveyorDirection, normal) > DirectionAmount)
                {
                    direction = Vector2.Zero;
                    AddBlocker(contact.OtherEnt(entity.Owner));
                    return false;
                }
            }

            _ray.CastShape(xform.GridUid.Value,
                ref result,
                fixture.Shape,
                transform,
                direction,
                filter,
                RayCastSystem.RayCastClosestCallback);

            if (result.Hit)
            {
                foreach (var (uid, localNormal, fraction) in result.Results)
                {
                    if (Vector2.Dot(conveyorDirection, localNormal) > DirectionAmount)
                    {
                        continue;
                    }

                    if (fraction < 1f)
                    {
                        // Can't go all the way so move partially and stop.
                        direction *= fraction;

                        AddBlocker(uid);

                        return false;
                    }
                }
            }
        }

        return true;
    }

    private void AddBlocker(EntityUid uid)
    {
        var blocker = EnsureComp<ConveyorBlockerComponent>(uid);

        if (blocker.Expired)
        {
            blocker.Expired = false;
            _expiredBlockers.Remove((uid, blocker));
        }
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

    public bool CanRun(ConveyorComponent component)
    {
        return component.State != ConveyorState.Off && component.Powered;
    }

    private record struct ConveyorJob : IParallelRobustJob
    {
        public int BatchSize => 16;

        public List<(Entity<ConveyedComponent, FixturesComponent, PhysicsComponent, TransformComponent> Entity, Vector2 Direction, bool Result)> Conveyed = new();
        public List<HashSet<Entity<ConveyorComponent>>> Conveyors = new();

        public SharedConveyorController System;

        public float FrameTime;
        public bool Prediction;

        public ConveyorJob(SharedConveyorController controller)
        {
            System = controller;
        }

        public void Execute(int index)
        {
            var conveyors = Conveyors[index];
            conveyors.Clear();

            var convey = Conveyed[index];

            var result = System.TryConvey(
                (convey.Entity.Owner, convey.Entity.Comp1, convey.Entity.Comp2, convey.Entity.Comp3, convey.Entity.Comp4),
                conveyors, Prediction, FrameTime, out var direction);

            Conveyed[index] = (convey.Entity, direction, result);
        }
    }
}
