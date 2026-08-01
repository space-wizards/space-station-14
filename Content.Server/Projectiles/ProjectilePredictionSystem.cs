using System.Numerics;
using Content.Server.Movement.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DeadSpace.CCCCVars;
using Content.Shared.GameTicking;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Projectiles;

/// <summary>
/// Validates collisions reported by locally simulated projectiles against recent server positions.
/// </summary>
public sealed class ProjectilePredictionSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly ProjectileSystem _projectiles = default!;
    [Dependency] private readonly RayCastSystem _rayCast = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RequireProjectileTargetSystem _requireProjectileTarget = default!;

    private readonly Dictionary<(NetUserId User, uint Id, ushort Index), EntityUid> _predicted = new();
    private readonly List<(PredictedProjectileHitEvent Event, ICommonSession Player)> _predictedHits = new();
    private readonly HashSet<EntityUid> _blockerCandidates = new();

    private bool _preventCollision;
    private bool _logHits;
    private float _coordinateDeviation;
    private float _lowestCoordinateDeviation;
    private float _aabbEnlargement;
    private bool _checkingPreventCollide;

    private const float ContactTolerance = 0.25f;
    private const float MinimumDirectionDot = 0.95f;

    private EntityQuery<FixturesComponent> _fixturesQuery;
    private EntityQuery<LagCompensationComponent> _lagCompensationQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<ProjectileComponent> _projectileQuery;
    private EntityQuery<TransformComponent> _transformQuery;

    public override void Initialize()
    {
        base.Initialize();

        _fixturesQuery = GetEntityQuery<FixturesComponent>();
        _lagCompensationQuery = GetEntityQuery<LagCompensationComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _projectileQuery = GetEntityQuery<ProjectileComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeNetworkEvent<PredictedProjectileHitEvent>(OnPredictedProjectileHit);
        SubscribeLocalEvent<PredictedProjectileComponent, PreventCollideEvent>(OnPredictedPreventCollide);
        SubscribeLocalEvent<PredictedProjectileComponent, ComponentRemove>(OnPredictedRemove);
        SubscribeLocalEvent<PredictedProjectileComponent, EntityTerminatingEvent>(OnPredictedRemove);

        Subs.CVar(_config, CCCCVars.ProjectilePredictionPreventCollision, value => _preventCollision = value, true);
        Subs.CVar(_config, CCCCVars.ProjectilePredictionLogHits, value => _logHits = value, true);
        Subs.CVar(_config, CCCCVars.ProjectilePredictionCoordinateDeviation, value => _coordinateDeviation = value, true);
        Subs.CVar(_config, CCCCVars.ProjectilePredictionLowestCoordinateDeviation, value => _lowestCoordinateDeviation = value, true);
        Subs.CVar(_config, CCCCVars.ProjectilePredictionAabbEnlargement, value => _aabbEnlargement = value, true);

        // Process client hit reports before authoritative physics can resolve the same collision.
        UpdatesBefore.Add(typeof(SharedPhysicsSystem));
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _predicted.Clear();
        _predictedHits.Clear();
    }

    private void OnPredictedProjectileHit(PredictedProjectileHitEvent ev, EntitySessionEventArgs args)
    {
        if (ev.Hits.Count == 0 || ev.Hits.Count > 16)
            return;

        _predictedHits.Add((ev, args.SenderSession));
    }

    private void OnPredictedRemove<T>(Entity<PredictedProjectileComponent> ent, ref T args)
    {
        if (TryComp<ActorComponent>(ent.Comp.Shooter, out var actor))
        {
            var key = (actor.PlayerSession.UserId, ent.Comp.PredictionId, ent.Comp.ProjectileIndex);
            if (_predicted.GetValueOrDefault(key) == ent.Owner)
                _predicted.Remove(key);
            return;
        }

        (NetUserId User, uint Id, ushort Index)? staleKey = null;
        foreach (var pair in _predicted)
        {
            if (pair.Value != ent.Owner)
                continue;

            staleKey = pair.Key;
            break;
        }

        if (staleKey != null)
            _predicted.Remove(staleKey.Value);
    }

    private void OnPredictedPreventCollide(Entity<PredictedProjectileComponent> ent, ref PreventCollideEvent args)
    {
        if (!_preventCollision || _checkingPreventCollide || args.Cancelled)
            return;

        if (!_lagCompensationQuery.TryComp(args.OtherEntity, out var lagCompensation) ||
            !_fixturesQuery.TryComp(args.OtherEntity, out var fixtures) ||
            !_physicsQuery.TryComp(args.OtherEntity, out var physics) ||
            !_transformQuery.TryComp(args.OtherEntity, out var xform) ||
            !_physicsQuery.TryComp(ent, out var projectilePhysics))
        {
            return;
        }

        if (!Collides(
                (ent, ent.Comp, projectilePhysics),
                (args.OtherEntity, lagCompensation, fixtures, physics, xform),
                null))
        {
            args.Cancelled = true;
        }
    }

    private bool Collides(
        Entity<PredictedProjectileComponent, PhysicsComponent> projectile,
        Entity<LagCompensationComponent, FixturesComponent, PhysicsComponent, TransformComponent> target,
        MapCoordinates? clientCoordinates)
    {
        var projectileCoordinates = _transform.GetMapCoordinates(projectile);
        var projectilePosition = projectileCoordinates.Position;

        var targetCoordinates = EntityCoordinates.Invalid;
        MapCoordinates? oldestPlausibleCoordinates = null;
        var ping = TryComp<ActorComponent>(projectile.Comp1.Shooter, out var actor)
            ? actor.PlayerSession.Channel.Ping
            : 0;
        var sentTime = _timing.CurTime - TimeSpan.FromMilliseconds(ping * 1.5);
        var pingTime = TimeSpan.FromMilliseconds(ping);

        foreach (var position in target.Comp1.Positions)
        {
            targetCoordinates = position.Item2;
            if (position.Item1 >= sentTime)
                break;

            if (oldestPlausibleCoordinates == null && position.Item1 >= sentTime - pingTime)
                oldestPlausibleCoordinates = _transform.ToMapCoordinates(position.Item2);
        }

        var targetMapCoordinates = targetCoordinates == EntityCoordinates.Invalid
            ? _transform.GetMapCoordinates(target)
            : _transform.ToMapCoordinates(targetCoordinates);

        if (clientCoordinates is { } reported &&
            (reported.InRange(targetMapCoordinates, _coordinateDeviation) ||
             oldestPlausibleCoordinates is { } oldest && reported.InRange(oldest, _lowestCoordinateDeviation)))
        {
            targetMapCoordinates = reported;
        }

        if (projectileCoordinates.MapId != targetMapCoordinates.MapId)
            return false;

        var transform = new Transform(targetMapCoordinates.Position, 0);
        var bounds = new Box2(transform.Position, transform.Position);
        var hasCollidingFixture = false;

        if (!_fixturesQuery.TryComp(projectile, out var projectileFixtures) ||
            !projectileFixtures.Fixtures.TryGetValue(SharedProjectileSystem.ProjectileFixture, out var projectileFixture))
        {
            return false;
        }

        foreach (var fixture in target.Comp2.Fixtures.Values)
        {
            if (!fixture.Hard || (fixture.CollisionLayer & projectileFixture.CollisionMask) == 0)
                continue;

            var preventCollide = new PreventCollideEvent(
                target,
                projectile,
                target.Comp3,
                projectile.Comp2,
                fixture,
                projectileFixture);
            RaiseLocalEvent(target, ref preventCollide);
            if (preventCollide.Cancelled)
                continue;

            hasCollidingFixture = true;

            for (var i = 0; i < fixture.Shape.ChildCount; i++)
                bounds = bounds.Union(fixture.Shape.ComputeAABB(transform, i));
        }

        if (!hasCollidingFixture)
            return false;

        bounds = bounds.Enlarged(_aabbEnlargement);
        if (bounds.Contains(projectilePosition))
            return true;

        var velocity = _physics.GetLinearVelocity(projectile, projectile.Comp2.LocalCenter);
        projectilePosition += velocity / _timing.TickRate / 1.5f;
        return bounds.Contains(projectilePosition);
    }

    private bool TryValidateReportedHit(
        Entity<PredictedProjectileComponent, PhysicsComponent> projectile,
        EntityUid target,
        FixturesComponent targetFixtures,
        PhysicsComponent targetPhysics,
        TransformComponent targetXform,
        MapCoordinates reportedTargetCoordinates,
        MapCoordinates reportedProjectileCoordinates,
        MapCoordinates reportedContactCoordinates,
        out EntityUid hitEntity,
        out MapCoordinates projectileCoordinates,
        out MapCoordinates hitCoordinates)
    {
        hitEntity = default;
        projectileCoordinates = default;
        hitCoordinates = default;

        var origin = projectile.Comp1.Origin;
        if (origin.MapId == MapId.Nullspace ||
            reportedTargetCoordinates.MapId != origin.MapId ||
            reportedProjectileCoordinates.MapId != origin.MapId ||
            reportedContactCoordinates.MapId != origin.MapId ||
            !_fixturesQuery.TryComp(projectile, out var projectileFixtures) ||
            !projectileFixtures.Fixtures.TryGetValue(
                SharedProjectileSystem.ProjectileFixture,
                out var projectileFixture) ||
            !TryGetValidatedTargetTransform(
                projectile,
                target,
                targetXform,
                reportedTargetCoordinates,
                out var targetCoordinates,
                out var targetAngle))
        {
            return false;
        }

        var targetTransform = new Transform(targetCoordinates.Position, targetAngle);
        Box2? targetBounds = null;
        foreach (var fixture in targetFixtures.Fixtures.Values)
        {
            if (!CanFixturesCollide(
                    projectile,
                    projectile.Comp2,
                    projectileFixture,
                    target,
                    targetPhysics,
                    fixture))
            {
                continue;
            }

            for (var i = 0; i < fixture.Shape.ChildCount; i++)
            {
                var fixtureBounds = fixture.Shape.ComputeAABB(targetTransform, i);
                targetBounds = targetBounds?.Union(fixtureBounds) ?? fixtureBounds;
            }
        }

        if (targetBounds is not { } bounds)
            return false;

        var castShape = GetCastShape(projectileFixture.Shape);
        var projectileAngle = projectileFixture.Shape is PhysShapeAabb
            ? Angle.Zero
            : _transform.GetWorldRotation(projectile.Owner);
        var reportedProjectileTransform = new Transform(reportedProjectileCoordinates.Position, projectileAngle);
        var reportedProjectileBounds = GetShapeBounds(castShape, reportedProjectileTransform);
        if (!bounds.Enlarged(ContactTolerance).Intersects(reportedProjectileBounds) ||
            !bounds.Enlarged(ContactTolerance).Contains(reportedContactCoordinates.Position))
        {
            return false;
        }

        var translation = reportedProjectileCoordinates.Position - origin.Position;
        var reportedDistance = translation.Length();
        var currentCoordinates = _transform.GetMapCoordinates(projectile);
        if (currentCoordinates.MapId != origin.MapId)
            return false;

        var velocity = _physics.GetLinearVelocity(projectile, projectile.Comp2.LocalCenter);
        var reachableDistance = (currentCoordinates.Position - origin.Position).Length() +
                                velocity.Length() / _timing.TickRate +
                                _aabbEnlargement;
        if (reportedDistance > reachableDistance)
            return false;

        if (reportedDistance > 0.001f && velocity.LengthSquared() > 0.001f &&
            Vector2.Dot(translation / reportedDistance, Vector2.Normalize(velocity)) < MinimumDirectionDot)
        {
            return false;
        }

        var filter = new QueryFilter
        {
            LayerBits = projectileFixture.CollisionLayer,
            MaskBits = projectileFixture.CollisionMask,
        };

        float BlockerCallback(
            FixtureProxy proxy,
            Vector2 point,
            Vector2 normal,
            float fraction,
            ref RayResult result)
        {
            if (proxy.Entity == projectile.Owner ||
                proxy.Entity == target ||
                !CanFixturesCollide(
                    projectile,
                    projectile.Comp2,
                    projectileFixture,
                    proxy.Entity,
                    proxy.Body,
                    proxy.Fixture))
            {
                return -1f;
            }

            return RayCastSystem.RayCastClosestCallback(proxy, point, normal, fraction, ref result);
        }

        var blockers = _rayCast.CastShape(
            origin.MapId,
            castShape,
            new Transform(origin.Position, projectileAngle),
            translation,
            filter,
            BlockerCallback);

        RayHit? closestBlocker = blockers.Hit ? blockers.Results[0] : null;

        // Fixture deserialization turns YAML AABBs into polygons with a skin radius. Robust routes those
        // through its known-broken GJK ray-vs-box path, so supplement blocker validation with swept SAT.
        var originTransform = new Transform(origin.Position, projectileAngle);
        var endTransform = new Transform(origin.Position + translation, projectileAngle);
        var lookupBounds = GetShapeBounds(castShape, originTransform)
            .Union(GetShapeBounds(castShape, endTransform));
        _blockerCandidates.Clear();
        _entityLookup.GetEntitiesIntersecting(
            origin.MapId,
            lookupBounds,
            _blockerCandidates,
            LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Approximate);

        foreach (var blockerCandidate in _blockerCandidates)
        {
            if (blockerCandidate == projectile.Owner ||
                blockerCandidate == target ||
                !_physicsQuery.TryComp(blockerCandidate, out var blockerBody) ||
                !_fixturesQuery.TryComp(blockerCandidate, out var blockerFixtures) ||
                !_transformQuery.TryComp(blockerCandidate, out var blockerXform))
            {
                continue;
            }

            var blockerTransform = _physics.GetPhysicsTransform(blockerCandidate, blockerXform);
            foreach (var blockerFixture in blockerFixtures.Fixtures.Values)
            {
                if (!CanFixturesCollide(
                        projectile,
                        projectile.Comp2,
                        projectileFixture,
                        blockerCandidate,
                        blockerBody,
                        blockerFixture))
                {
                    continue;
                }

                if (!SharedProjectileSystem.TryCastProjectileAgainstShape(
                        castShape,
                        projectileAngle,
                        origin.Position,
                        translation,
                        blockerFixture.Shape,
                        blockerTransform,
                        out var fraction,
                        out var contactPoint) ||
                    closestBlocker is { } current && fraction >= current.Fraction)
                {
                    continue;
                }

                closestBlocker = new RayHit(blockerCandidate, Vector2.Zero, fraction)
                {
                    Point = contactPoint,
                };
            }
        }

        if (closestBlocker is { } blocker)
        {
            hitEntity = blocker.Entity;
            projectileCoordinates = new MapCoordinates(
                origin.Position + translation * blocker.Fraction,
                origin.MapId);
            hitCoordinates = new MapCoordinates(blocker.Point, origin.MapId);
            return true;
        }

        hitEntity = target;
        projectileCoordinates = reportedProjectileCoordinates;
        hitCoordinates = reportedContactCoordinates;
        return true;
    }

    private bool TryGetValidatedTargetTransform(
        Entity<PredictedProjectileComponent> projectile,
        EntityUid target,
        TransformComponent targetXform,
        MapCoordinates reportedCoordinates,
        out MapCoordinates targetCoordinates,
        out Angle targetAngle)
    {
        targetCoordinates = _transform.GetMapCoordinates(target, targetXform);
        targetAngle = _transform.GetWorldRotation(targetXform);
        if (targetCoordinates.MapId != reportedCoordinates.MapId)
            return false;

        if (!_lagCompensationQuery.TryComp(target, out var lagCompensation) ||
            lagCompensation.Positions.Count == 0)
        {
            return reportedCoordinates.InRange(targetCoordinates, _coordinateDeviation);
        }

        var ping = TryComp<ActorComponent>(projectile.Comp.Shooter, out var actor)
            ? actor.PlayerSession.Channel.Ping
            : 0;
        var pingTime = TimeSpan.FromMilliseconds(ping);
        var sentTime = _timing.CurTime - TimeSpan.FromMilliseconds(ping * 1.5);

        (TimeSpan Time, EntityCoordinates Coordinates, Angle Angle)? baseline = null;
        foreach (var position in lagCompensation.Positions)
        {
            baseline = position;
            if (position.Item1 >= sentTime)
                break;
        }

        if (baseline is { } expected)
        {
            var expectedCoordinates = _transform.ToMapCoordinates(expected.Coordinates);
            if (expectedCoordinates.MapId == reportedCoordinates.MapId &&
                reportedCoordinates.InRange(expectedCoordinates, _coordinateDeviation))
            {
                targetCoordinates = expectedCoordinates;
                targetAngle = expected.Angle;
                return true;
            }
        }

        var earliest = sentTime - pingTime;
        var latest = sentTime + pingTime;
        var bestDistance = float.MaxValue;
        MapCoordinates? bestCoordinates = null;
        var bestAngle = Angle.Zero;
        foreach (var position in lagCompensation.Positions)
        {
            if (position.Item1 < earliest || position.Item1 > latest)
                continue;

            var coordinates = _transform.ToMapCoordinates(position.Item2);
            if (coordinates.MapId != reportedCoordinates.MapId)
                continue;

            var distance = (coordinates.Position - reportedCoordinates.Position).Length();
            if (distance > _lowestCoordinateDeviation || distance >= bestDistance)
                continue;

            bestDistance = distance;
            bestCoordinates = coordinates;
            bestAngle = position.Item3;
        }

        if (bestCoordinates is not { } best)
            return false;

        targetCoordinates = best;
        targetAngle = bestAngle;
        return true;
    }

    private bool CanFixturesCollide(
        EntityUid projectile,
        PhysicsComponent projectilePhysics,
        Fixture projectileFixture,
        EntityUid target,
        PhysicsComponent targetPhysics,
        Fixture targetFixture)
    {
        if (!targetFixture.Hard ||
            ((targetFixture.CollisionLayer & projectileFixture.CollisionMask) == 0 &&
             (targetFixture.CollisionMask & projectileFixture.CollisionLayer) == 0))
        {
            return false;
        }

        _checkingPreventCollide = true;
        try
        {
            var projectileEvent = new PreventCollideEvent(
                projectile,
                target,
                projectilePhysics,
                targetPhysics,
                projectileFixture,
                targetFixture);
            RaiseLocalEvent(projectile, ref projectileEvent);
            if (projectileEvent.Cancelled)
                return false;

            var targetEvent = new PreventCollideEvent(
                target,
                projectile,
                targetPhysics,
                projectilePhysics,
                targetFixture,
                projectileFixture);
            RaiseLocalEvent(target, ref targetEvent);
            return !targetEvent.Cancelled;
        }
        finally
        {
            _checkingPreventCollide = false;
        }
    }

    private static IPhysShape GetCastShape(IPhysShape shape)
    {
        if (shape is PhysShapeCircle or PolygonShape)
            return shape;

        var bounds = shape.ComputeAABB(Robust.Shared.Physics.Transform.Empty, 0);
        for (var i = 1; i < shape.ChildCount; i++)
            bounds = bounds.Union(shape.ComputeAABB(Robust.Shared.Physics.Transform.Empty, i));

        var polygon = new PolygonShape();
        polygon.SetAsBox(bounds);
        return polygon;
    }

    private static Box2 GetShapeBounds(IPhysShape shape, Transform transform)
    {
        var bounds = shape.ComputeAABB(transform, 0);
        for (var i = 1; i < shape.ChildCount; i++)
            bounds = bounds.Union(shape.ComputeAABB(transform, i));

        return bounds;
    }

    private void ProcessPredictedHit(PredictedProjectileHitEvent ev, ICommonSession player)
    {
        if (!_predicted.TryGetValue((player.UserId, ev.PredictionId, ev.ProjectileIndex), out var projectile) ||
            !TryComp(projectile, out PredictedProjectileComponent? predicted) ||
            predicted.Hit ||
            !_projectileQuery.TryComp(projectile, out var projectileComponent) ||
            !_physicsQuery.TryComp(projectile, out var projectilePhysics))
        {
            return;
        }

        if (!TryComp<ActorComponent>(predicted.Shooter, out var shooter) || shooter.PlayerSession.UserId != player.UserId)
            return;

        var accepted = false;
        foreach (var (
                     netEntity,
                     targetCoordinates,
                     projectileCoordinates,
                     contactCoordinates) in ev.Hits)
        {
            var target = GetEntity(netEntity);
            if (!target.Valid ||
                !_fixturesQuery.TryComp(target, out var fixtures) ||
                !_physicsQuery.TryComp(target, out var physics) ||
                !_transformQuery.TryComp(target, out var xform))
            {
                continue;
            }

            if (projectileComponent.IgnoreShooter &&
                (target == projectileComponent.Shooter || target == projectileComponent.Weapon) &&
                (!TryComp(projectile, out TargetedProjectileComponent? ignoredTarget) ||
                 ignoredTarget.Target != target))
            {
                continue;
            }

            if (TryComp(target, out RequireProjectileTargetComponent? requireTarget) &&
                _requireProjectileTarget.RequiresExplicitTarget((target, requireTarget)) &&
                (!TryComp(projectile, out TargetedProjectileComponent? targeted) || targeted.Target != target))
            {
                continue;
            }

            if (!TryValidateReportedHit(
                    (projectile, predicted, projectilePhysics),
                    target,
                    fixtures,
                    physics,
                    xform,
                    targetCoordinates,
                    projectileCoordinates,
                    contactCoordinates,
                    out var validatedTarget,
                    out var validatedProjectile,
                    out var validatedContact))
            {
                if (_logHits)
                    Log.Info($"Rejected predicted hit from {player.Name} on {ToPrettyString(target)}");
                continue;
            }

            if (_logHits)
                Log.Info($"Accepted predicted hit from {player.Name} on {ToPrettyString(target)}");

            var corrected = validatedTarget != target;
            if (corrected)
                _projectiles.ReconcilePredictedProjectile(projectile);

            predicted.Hit = true;
            // Trigger-on-hit effects (portals, spawned effects, grenades, etc.) read the projectile's
            // transform. Move it to the validated collision pose before raising ProjectileHitEvent so
            // those effects cannot appear at the latency-delayed authoritative position.
            _transform.SetMapCoordinates(projectile, validatedProjectile);
            _projectiles.ProjectileCollide(
                (projectile, projectileComponent, projectilePhysics),
                validatedTarget,
                true,
                validatedContact,
                !corrected);
            accepted = true;
            break;
        }

        if (!accepted)
            _projectiles.ReconcilePredictedProjectile(projectile);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var predictedQuery = EntityQueryEnumerator<PredictedProjectileComponent>();
        while (predictedQuery.MoveNext(out var uid, out var predicted))
        {
            if (predicted.PredictionId == 0 ||
                !TryComp<ActorComponent>(predicted.Shooter, out var actor))
            {
                continue;
            }

            _predicted[(actor.PlayerSession.UserId, predicted.PredictionId, predicted.ProjectileIndex)] = uid;
        }

        try
        {
            foreach (var hit in _predictedHits)
                ProcessPredictedHit(hit.Event, hit.Player);
        }
        finally
        {
            _predictedHits.Clear();
        }

        var hitQuery = EntityQueryEnumerator<PredictedProjectileHitComponent, TransformComponent>();
        while (hitQuery.MoveNext(out var uid, out var hit, out var xform))
        {
            if (!hit.Origin.TryDistance(EntityManager, _transform, xform.Coordinates, out var distance) ||
                distance >= hit.Distance)
            {
                QueueDel(uid);
            }
        }
    }
}
