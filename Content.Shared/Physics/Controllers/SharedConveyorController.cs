using System.Numerics;
using Content.Shared.Conveyor;
using Content.Shared.Gravity;
using Content.Shared.Movement.Events;
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
using Robust.Shared.Timing;

namespace Content.Shared.Physics.Controllers;

public abstract class SharedConveyorController : VirtualController
{
    /*
     * Conveyors move entities directly rather than using velocity as we don't want to interfere with other sources
     * of velocity, e.g. throwing, so we don't have to worry about tracking velocity sources every tick.
     *
     * This means we need a slim version of physics to handle this so the below handles:
     * - Conveyor sleeping
     * - Conveyor waking
     */

    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] private   readonly IParallelManager _parallel = default!;
    [Dependency] private   readonly CollisionWakeSystem _wake = default!;
    [Dependency] protected readonly EntityLookupSystem Lookup = default!;
    [Dependency] private   readonly FixtureSystem _fixtures = default!;
    [Dependency] private   readonly RayCastSystem _ray = default!;
    [Dependency] private   readonly SharedGravitySystem _gravity = default!;
    [Dependency] private   readonly SharedMoverController _mover = default!;

    protected const string ConveyorFixture = "conveyor";

    private ConveyorJob _job;

    private EntityQuery<ConveyorComponent> _conveyorQuery;
    private EntityQuery<ConveyedComponent> _conveyedQuery;
    protected EntityQuery<PhysicsComponent> PhysicsQuery;
    protected EntityQuery<TransformComponent> XformQuery;

    protected HashSet<EntityUid> Intersecting = new();

    private ValueList<EntityUid> _ents = new();

    /// <summary>
    /// How much a conveyed item needs to move in the desired direction to avoid sleeping.
    /// </summary>
    private const float DirectionAmount = 0.3f;

    public override void Initialize()
    {
        _job = new ConveyorJob(this);
        _conveyorQuery = GetEntityQuery<ConveyorComponent>();
        _conveyedQuery = GetEntityQuery<ConveyedComponent>();
        PhysicsQuery = GetEntityQuery<PhysicsComponent>();
        XformQuery = GetEntityQuery<TransformComponent>();

        UpdatesAfter.Add(typeof(SharedMoverController));

        SubscribeLocalEvent<ActiveConveyedComponent, TileFrictionEvent>(OnConveyedFriction);

        SubscribeLocalEvent<ConveyedComponent, PhysicsWakeEvent>(OnConveyedWake);
        SubscribeLocalEvent<ConveyedComponent, PhysicsSleepEvent>(OnConveyedSleep);
        SubscribeLocalEvent<ConveyedComponent, ComponentStartup>(OnConveyedStartup);
        SubscribeLocalEvent<ConveyedComponent, ComponentShutdown>(OnConveyedShutdown);


        SubscribeLocalEvent<ConveyorComponent, StartCollideEvent>(OnConveyorStartCollide);
        SubscribeLocalEvent<ConveyorComponent, ComponentStartup>(OnConveyorStartup);

        base.Initialize();
    }

    private void OnConveyedFriction(Entity<ActiveConveyedComponent> ent, ref TileFrictionEvent args)
    {
        // Conveyed entities don't get friction, they just get wishdir applied so will inherently slowdown anyway.
        args.Modifier = 0f;
    }

    private void OnConveyedStartup(Entity<ConveyedComponent> ent, ref ComponentStartup args)
    {
        // We need waking / sleeping to work and don't want collisionwake interfering with us.
        EnsureComp<ActiveConveyedComponent>(ent.Owner);
        _wake.SetEnabled(ent.Owner, false);
    }

    private void OnConveyedShutdown(Entity<ConveyedComponent> ent, ref ComponentShutdown args)
    {
        RemCompDeferred<ActiveConveyedComponent>(ent.Owner);
        _wake.SetEnabled(ent.Owner, true);
    }

    private void OnConveyedWake(Entity<ConveyedComponent> ent, ref PhysicsWakeEvent args)
    {
        EnsureComp<ActiveConveyedComponent>(ent);
    }

    private void OnConveyedSleep(Entity<ConveyedComponent> ent, ref PhysicsSleepEvent args)
    {
        RemCompDeferred<ActiveConveyedComponent>(ent);
    }

    private void OnConveyorStartup(Entity<ConveyorComponent> ent, ref ComponentStartup args)
    {
        AwakenConveyor(ent.Owner);
    }

    /// <summary>
    /// Forcefully awakens all entities near the conveyor.
    /// </summary>
    protected virtual void AwakenConveyor(Entity<TransformComponent?> ent)
    {
    }

    /// <summary>
    /// Wakes all conveyed entities contacting this conveyor.
    /// </summary>
    protected void WakeConveyed(EntityUid conveyorUid)
    {
        var contacts = PhysicsSystem.GetContacts(conveyorUid);

        while (contacts.MoveNext(out var contact))
        {
            var other = contact.OtherEnt(conveyorUid);

            if (_conveyedQuery.HasComp(other))
            {
                PhysicsSystem.WakeBody(other);
            }
        }
    }

    private void OnConveyorStartCollide(EntityUid uid, ConveyorComponent component, ref StartCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        if (!args.OtherFixture.Hard || args.OtherBody.BodyType == BodyType.Static)
            return;

        EnsureComp<ActiveConveyedComponent>(otherUid);
        EnsureComp<ConveyedComponent>(otherUid);
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        _job.FrameTime = frameTime;
        _job.Prediction = prediction;
        _job.Conveyed.Clear();
        _ents.Clear();

        var query = EntityQueryEnumerator<ActiveConveyedComponent, ConveyedComponent, FixturesComponent, PhysicsComponent>();

            // Not conveying anywhere.
            if (ent.Direction.Equals(Vector2.Zero))
            {
                continue;
            }

            var physics = ent.Entity.Comp3;
            var velocity = physics.LinearVelocity;

            // If mob is moving with the conveyor then combine the directions.
            var targetDir = ent.Direction;
            var wishDir = _mover.GetWishDir(ent.Entity.Owner);

            if (Vector2.Dot(wishDir, targetDir) > 0f)
            {
                targetDir += wishDir;
            }

            SharedMoverController.Accelerate(ref velocity, targetDir, 20f, frameTime);

            PhysicsSystem.SetLinearVelocity(ent.Entity.Owner, velocity);
        }

        foreach (var ent in _ents)
        {
            RemComp<ConveyedComponent>(ent);
        }
    }

    /// <summary>
    /// Gets the conveying direction for an entity.
    /// </summary>
    /// <returns>False if we should no longer be considered conveyed</returns>
    private bool TryConvey(Entity<ConveyedComponent, FixturesComponent, PhysicsComponent, TransformComponent> entity,
        bool prediction,
        float frameTime,
        out Vector2 direction)
    {
        direction = Vector2.Zero;
        var fixtures = entity.Comp2;
        var physics = entity.Comp3;
        var xform = entity.Comp4;

        // Client moment
        if (!physics.Predict && prediction)
            return true;

        if (xform.GridUid == null)
            return true;

        if (physics.BodyStatus == BodyStatus.InAir ||
            _gravity.IsWeightless(entity, physics, xform))
        {
            return true;
        }

        Entity<ConveyorComponent> bestConveyor = default;
        var bestSpeed = 0f;
        var contacts = PhysicsSystem.GetContacts((entity.Owner, fixtures));
        var worldPos = TransformSystem.GetWorldPosition(entity.Owner);
        var anyConveyors = false;

        while (contacts.MoveNext(out var contact))
        {
            if (!contact.IsTouching)
                continue;

            // Check if our center is over their fixture otherwise ignore it.
            var other = contact.OtherEnt(entity.Owner);

            if (!_conveyorQuery.TryComp(other, out var conveyor))
                continue;

            anyConveyors = true;
            var otherFixture = contact.OtherFixture(entity.Owner);
            var otherWorldPos = PhysicsSystem.GetPhysicsTransform(other);

            if (!_fixtures.TestPoint(otherFixture.Item2.Shape, otherWorldPos, worldPos))
                continue;

            if (conveyor.Speed > bestSpeed && CanRun(conveyor))
            {
                bestSpeed = conveyor.Speed;
                bestConveyor = (other, conveyor);
            }
        }

        // If we have no touching contacts we shouldn't be using conveyed anyway so nuke it.
        if (!anyConveyors)
            return false;

        if (bestSpeed == 0f || bestConveyor == default)
            return true;

        var comp = bestConveyor.Comp!;
        var conveyorXform = XformQuery.GetComponent(bestConveyor.Owner);
        var (conveyorPos, conveyorRot) = TransformSystem.GetWorldPositionRotation(conveyorXform);

        conveyorRot += bestConveyor.Comp!.Angle;

        if (comp.State == ConveyorState.Reverse)
            conveyorRot += MathF.PI;

        var conveyorDirection = conveyorRot.ToWorldVec();
        direction = conveyorDirection;

        var itemRelative = conveyorPos - TransformSystem.GetWorldPosition(xform);

        // TODO: Remove frametime from convey
        direction = Convey(direction, bestSpeed, frameTime, itemRelative) / frameTime;

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

            foreach (var contact in fixture.Contacts.Values)
            {
                var normal = contact.Manifold.LocalNormal;

                // If it's overlapping already and we're not conveying in that direction.
                if (contact.IsTouching && contact.Hard && Vector2.Dot(conveyorDirection, normal) > DirectionAmount)
                {
                    direction = Vector2.Zero;
                    return true;
                }
            }

            _ray.CastShape(xform.GridUid.Value,
                ref result,
                fixture.Shape,
                transform,
                direction * frameTime,
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
                        return true;
                    }
                }
            }
        }

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

        public List<(Entity<ConveyedComponent, FixturesComponent, PhysicsComponent, TransformComponent> Entity, Vector2 Direction, bool Result)> Conveyed = new();

        public SharedConveyorController System;

        public float FrameTime;
        public bool Prediction;

        public ConveyorJob(SharedConveyorController controller)
        {
            System = controller;
        }

        public void Execute(int index)
        {
            var convey = Conveyed[index];

            var result = System.TryConvey(
                (convey.Entity.Owner, convey.Entity.Comp1, convey.Entity.Comp2, convey.Entity.Comp3, convey.Entity.Comp4),
                Prediction, FrameTime, out var direction);

            Conveyed[index] = (convey.Entity, direction, result);
        }
    }

    /// <summary>
    /// Checks an entity's contacts to see if it's still being conveyed.
    /// </summary>
    private bool IsConveyed(Entity<FixturesComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        var contacts = PhysicsSystem.GetContacts(ent.Owner);

        while (contacts.MoveNext(out var contact))
        {
            if (!contact.IsTouching)
                continue;

            var other = contact.OtherEnt(ent.Owner);

            if (_conveyorQuery.HasComp(other))
                return true;
        }

        return false;
    }
}
