using System.Numerics;
using Content.Shared.Conveyor;
using Content.Shared.Gravity;
using Content.Shared.Magic;
using Content.Shared.Movement.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Threading;
using Robust.Shared.Utility;

namespace Content.Shared.Physics.Controllers;

public abstract class SharedConveyorController : VirtualController
{
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] private   readonly IParallelManager _parallel = default!;
    [Dependency] protected readonly EntityLookupSystem Lookup = default!;
    [Dependency] private   readonly SharedMapSystem _maps = default!;
    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
    [Dependency] private   readonly SharedGravitySystem _gravity = default!;

    protected const string ConveyorFixture = "conveyor";

    private ConveyorJob _job;

    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private ValueList<EntityUid> _ents = new();

    public override void Initialize()
    {
        _job = new ConveyorJob(this);
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        UpdatesAfter.Add(typeof(SharedMoverController));

        SubscribeLocalEvent<ConveyorComponent, StartCollideEvent>(OnConveyorStartCollide);
        SubscribeLocalEvent<ConveyorComponent, EndCollideEvent>(OnConveyorEndCollide);

        base.Initialize();
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

        _job.FrameTime = frameTime;
        _job.Prediction = prediction;
        _job.Conveyed.Clear();
        _ents.Clear();

        var query = EntityQueryEnumerator<ConveyedComponent, PhysicsComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var body, out var xform))
        {
            _job.Conveyed.Add(((uid, comp, body, xform), Vector2.Zero, Angle.Zero, false));
        }

        for (var i = _job.Intersecting.Count; i < _job.Conveyed.Count; i++)
        {
            _job.Intersecting.Add(new HashSet<Entity<ConveyorComponent>>());
        }

        _parallel.ProcessNow(_job, _job.Conveyed.Count);

        foreach (var ent in _job.Conveyed)
        {
            if (!ent.Result)
            {
                _ents.Add(ent.Entity.Owner);
                continue;
            }

            // If local position still moving then don't sleep it.
            if (ent.Entity.Comp1.LastPosition != null)
            {
                var diff = (ent.Entity.Comp3.LocalPosition - ent.Entity.Comp1.LastPosition.Value);
                var conveyorAngle = ent.ConveyorRot.ToWorldVec();
                var dotProduct = Vector2.Dot(diff.Normalized(), conveyorAngle);

                // If it's not moving in the intended direction then stop waking it.
                if (diff.Equals(Vector2.Zero) || dotProduct < 0.3f)
                {
                    ent.Entity.Comp1.StopTimer += frameTime;
                    //_ents.Add(ent.Entity.Owner);
                    //continue;
                }
                else
                {
                    ent.Entity.Comp1.StopTimer = 0f;
                }
            }

            if (ent.Entity.Comp1.StopTimer > 0.5f)
            {
                _ents.Add(ent.Entity.Owner);
                continue;
            }

            ent.Entity.Comp1.LastPosition = ent.Entity.Comp3.LocalPosition;
            // Force it awake for collisionwake reasons.
            Physics.SetAwake((ent.Entity.Owner, ent.Entity.Comp2), true);
            Physics.SetSleepTime(ent.Entity.Comp2, 0f);

            TransformSystem.SetLocalPosition(ent.Entity.Owner, ent.Entity.Comp3.LocalPosition + ent.Direction, ent.Entity.Comp3);
        }

        foreach (var ent in _ents)
        {
            RemComp<ConveyedComponent>(ent);
        }
    }

    private bool TryConvey(Entity<ConveyedComponent, PhysicsComponent, TransformComponent> entity,
        HashSet<Entity<ConveyorComponent>> intersecting,
        bool prediction,
        float frameTime,
        out Vector2 direction,
        out Angle conveyorRot)
    {
        conveyorRot = Angle.Zero;
        direction = Vector2.Zero;
        var physics = entity.Comp2;
        var xform = entity.Comp3;
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
        Lookup.GetLocalEntitiesIntersecting(xform.GridUid.Value, gridTile, intersecting, gridComp: grid,
            flags: LookupFlags.Static | LookupFlags.Sensors | LookupFlags.Approximate);

        // No more conveyors.
        if (intersecting.Count == 0)
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

        foreach (var conveyor in intersecting)
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
        var conveyorXform = _xformQuery.GetComponent(bestConveyor.Owner);
        var conveyorPos = conveyorXform.LocalPosition;
        conveyorRot = conveyorXform.LocalRotation;

        conveyorRot += bestConveyor.Comp!.Angle;

        if (comp.State == ConveyorState.Reverse)
            conveyorRot += MathF.PI;

        direction = conveyorRot.ToWorldVec();

        var itemRelative = conveyorPos - xform.LocalPosition;

        direction = Convey(direction, bestSpeed, frameTime, itemRelative);

        return true;
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

        public List<(Entity<ConveyedComponent, PhysicsComponent, TransformComponent> Entity, Vector2 Direction, Angle ConveyorRot, bool Result)> Conveyed = new();
        public List<HashSet<Entity<ConveyorComponent>>> Intersecting = new();

        public SharedConveyorController System;

        public float FrameTime;
        public bool Prediction;

        public ConveyorJob(SharedConveyorController controller)
        {
            System = controller;
        }

        public void Execute(int index)
        {
            var intersecting = Intersecting[index];
            intersecting.Clear();

            var convey = Conveyed[index];

            var result = System.TryConvey((convey.Entity.Owner, convey.Entity.Comp1, convey.Entity.Comp2, convey.Entity.Comp3),
                intersecting, Prediction, FrameTime, out var direction, out var conveyorRot);

            Conveyed[index] = (convey.Entity, direction, conveyorRot, result);
        }
    }
}
