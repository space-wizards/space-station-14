using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Destructible;
using Content.Server.Effects;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DeadSpace.Player;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Projectiles;

public sealed class ProjectileSystem : SharedProjectileSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ColorFlashEffectSystem _color = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly DestructibleSystem _destructibleSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly GunSystem _guns = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly RayCastSystem _rayCast = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Dictionary<EntityUid, MapCoordinates> _positionsBeforeSolve = new();
    private readonly Dictionary<EntityUid, Dictionary<EntityUid, MapCoordinates>> _targetPositionsBeforeSolve = new();
    private readonly HashSet<EntityUid> _sweepCandidates = new();

    // This is only used to find nearby moving fixtures before the physics step. The actual hit test uses
    // their measured displacement, so this does not enlarge the projectile or create false positives.
    private const float MovingTargetLookupSpeed = 80f;

    private EntityQuery<FixturesComponent> _fixturesQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<TransformComponent> _transformQuery;

    public override void Initialize()
    {
        base.Initialize();
        _fixturesQuery = GetEntityQuery<FixturesComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<ProjectileComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ProjectileComponent, ComponentShutdown>(OnProjectileShutdown);
        SubscribeLocalEvent<PhysicsUpdateBeforeSolveEvent>(OnBeforePhysicsSolve);
        SubscribeLocalEvent<PhysicsUpdateAfterSolveEvent>(OnAfterPhysicsSolve);
    }

    private void OnProjectileShutdown(Entity<ProjectileComponent> ent, ref ComponentShutdown args)
    {
        _positionsBeforeSolve.Remove(ent.Owner);
        _targetPositionsBeforeSolve.Remove(ent.Owner);
    }

    private void OnBeforePhysicsSolve(ref PhysicsUpdateBeforeSolveEvent args)
    {
        _positionsBeforeSolve.Clear();

        var query = EntityQueryEnumerator<ProjectileComponent, PhysicsComponent, FixturesComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var projectile, out var projectileBody, out var fixtures, out var xform))
        {
            if (projectile.ProjectileSpent ||
                projectile is { Weapon: null, OnlyCollideWhenShot: true } ||
                !fixtures.Fixtures.TryGetValue(ProjectileFixture, out var projectileFixture))
            {
                continue;
            }

            var coordinates = _transform.GetMapCoordinates(uid, xform);
            if (coordinates.MapId == MapId.Nullspace)
                continue;

            _positionsBeforeSolve[uid] = coordinates;

            if (!_targetPositionsBeforeSolve.TryGetValue(uid, out var targetPositions))
            {
                targetPositions = new Dictionary<EntityUid, MapCoordinates>();
                _targetPositionsBeforeSolve.Add(uid, targetPositions);
            }

            targetPositions.Clear();
            _sweepCandidates.Clear();

            var expectedTranslation = _physics.GetMapLinearVelocity(uid, projectileBody, xform) * args.DeltaTime;
            if (expectedTranslation.LengthSquared() < 0.000001f)
                continue;

            var castShape = GetProjectileCastShape(projectileFixture.Shape);
            var projectileAngle = projectileFixture.Shape is PhysShapeAabb
                ? Angle.Zero
                : _transform.GetWorldRotation(xform);
            var startTransform = new Robust.Shared.Physics.Transform(coordinates.Position, projectileAngle);
            var endTransform = new Robust.Shared.Physics.Transform(
                coordinates.Position + expectedTranslation,
                projectileAngle);
            var lookupBounds = castShape.ComputeAABB(startTransform, 0)
                .Union(castShape.ComputeAABB(endTransform, 0))
                .Enlarged(MathF.Max(0.5f, MovingTargetLookupSpeed * args.DeltaTime));

            _entityLookup.GetEntitiesIntersecting(
                coordinates.MapId,
                lookupBounds,
                _sweepCandidates,
                LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Approximate);

            foreach (var target in _sweepCandidates)
            {
                if (target == uid ||
                    !_fixturesQuery.TryComp(target, out var targetFixtures) ||
                    !_physicsQuery.HasComp(target) ||
                    !_transformQuery.TryComp(target, out var targetXform))
                {
                    continue;
                }

                var hasCompatibleFixture = false;
                foreach (var targetFixture in targetFixtures.Fixtures.Values)
                {
                    if (!targetFixture.Hard ||
                        ((targetFixture.CollisionLayer & projectileFixture.CollisionMask) == 0 &&
                         (targetFixture.CollisionMask & projectileFixture.CollisionLayer) == 0))
                    {
                        continue;
                    }

                    hasCompatibleFixture = true;
                    break;
                }

                if (!hasCompatibleFixture)
                    continue;

                var targetCoordinates = _transform.GetMapCoordinates(target, targetXform);
                if (targetCoordinates.MapId == coordinates.MapId)
                    targetPositions[target] = targetCoordinates;
            }
        }
    }

    private void OnAfterPhysicsSolve(ref PhysicsUpdateAfterSolveEvent args)
    {
        foreach (var (uid, start) in _positionsBeforeSolve)
        {
            _targetPositionsBeforeSolve.TryGetValue(uid, out var targetStarts);
            SweepProjectile(uid, start, targetStarts);
        }

        _positionsBeforeSolve.Clear();
    }

    /// <summary>
    /// The engine currently has no continuous collision solver. Shape-cast the projectile fixture over
    /// the distance travelled by this physics substep so fast bullets cannot tunnel through a fixture.
    /// </summary>
    private void SweepProjectile(
        EntityUid uid,
        MapCoordinates start,
        Dictionary<EntityUid, MapCoordinates>? targetStarts)
    {
        if (!TryComp(uid, out ProjectileComponent? projectile) ||
            projectile.ProjectileSpent ||
            projectile is { Weapon: null, OnlyCollideWhenShot: true } ||
            !_physicsQuery.TryComp(uid, out var projectileBody) ||
            !_fixturesQuery.TryComp(uid, out var fixtures) ||
            !fixtures.Fixtures.TryGetValue(ProjectileFixture, out var projectileFixture) ||
            !_transformQuery.TryComp(uid, out var xform))
        {
            return;
        }

        var end = _transform.GetMapCoordinates(uid, xform);
        if (end.MapId != start.MapId)
            return;

        var translation = end.Position - start.Position;
        if (translation.LengthSquared() < 0.000001f)
            return;

        var castShape = GetProjectileCastShape(projectileFixture.Shape);
        var projectileAngle = projectileFixture.Shape is PhysShapeAabb
            ? Angle.Zero
            : _transform.GetWorldRotation(xform);
        var filter = new QueryFilter
        {
            LayerBits = projectileFixture.CollisionLayer,
            MaskBits = projectileFixture.CollisionMask,
        };

        bool TryGetTargetDisplacement(EntityUid target, out Vector2 displacement)
        {
            displacement = default;
            if (targetStarts == null ||
                !targetStarts.TryGetValue(target, out var targetStart) ||
                !_transformQuery.TryComp(target, out var targetXform))
            {
                return false;
            }

            var targetEnd = _transform.GetMapCoordinates(target, targetXform);
            if (targetEnd.MapId != targetStart.MapId)
                return false;

            displacement = targetEnd.Position - targetStart.Position;
            return displacement.LengthSquared() >= 0.000001f;
        }

        float SweepCallback(
            FixtureProxy proxy,
            Vector2 point,
            Vector2 normal,
            float fraction,
            ref RayResult result)
        {
            if (proxy.Entity == uid ||
                projectile.ProcessedTargets.Contains(proxy.Entity) ||
                TryGetTargetDisplacement(proxy.Entity, out _) ||
                !CanFixturesCollide(
                    uid,
                    projectileBody,
                    projectileFixture,
                    proxy.Entity,
                    proxy.Body,
                    proxy.Fixture))
            {
                return -1f;
            }

            return RayCastSystem.RayCastAllCallback(proxy, point, normal, fraction, ref result);
        }

        var result = _rayCast.CastShape(
            start.MapId,
            castShape,
            new Robust.Shared.Physics.Transform(start.Position, projectileAngle),
            translation,
            filter,
            SweepCallback);
        var hits = new List<RayHit>(result.Results.Count + (targetStarts?.Count ?? 0));
        foreach (var hit in result.Results)
            hits.Add(hit);

        // YAML AABBs become polygons with a skin radius and Robust sends those through its known-broken
        // GJK ray-vs-box path. Continuously test the captured blockers with the shared swept-SAT fallback.
        if (targetStarts != null)
        {
            foreach (var (target, targetStart) in targetStarts)
            {
                if (projectile.ProcessedTargets.Contains(target) ||
                    !_physicsQuery.TryComp(target, out var targetBody) ||
                    !_fixturesQuery.TryComp(target, out var targetFixtures) ||
                    !_transformQuery.TryComp(target, out var targetXform))
                {
                    continue;
                }

                var targetEnd = _transform.GetMapCoordinates(target, targetXform);
                if (targetEnd.MapId != start.MapId || targetStart.MapId != start.MapId)
                    continue;

                var targetDisplacement = targetEnd.Position - targetStart.Position;
                var targetTransform = _physics.GetPhysicsTransform(target, targetXform);
                var closestFraction = float.MaxValue;
                var closestPoint = Vector2.Zero;
                foreach (var targetFixture in targetFixtures.Fixtures.Values)
                {
                    if (!CanFixturesCollide(
                            uid,
                            projectileBody,
                            projectileFixture,
                            target,
                            targetBody,
                            targetFixture))
                    {
                        continue;
                    }

                    if (!TryCastProjectileAgainstShape(
                            castShape,
                            projectileAngle,
                            start.Position + targetDisplacement,
                            translation - targetDisplacement,
                            targetFixture.Shape,
                            targetTransform,
                            out var fraction,
                            out var contactPoint) ||
                        fraction >= closestFraction)
                    {
                        continue;
                    }

                    closestFraction = fraction;
                    closestPoint = contactPoint;
                }

                if (closestFraction == float.MaxValue)
                    continue;

                closestPoint -= targetDisplacement * (1f - closestFraction);
                hits.Add(new RayHit(target, Vector2.Zero, closestFraction)
                {
                    Point = closestPoint,
                });
            }
        }

        // A target can move into the projectile's starting shape between discrete samples. Shape casts
        // intentionally ignore initial overlaps, so cast the projectile in each moving target's frame.
        // This gives the continuous relative path of both bodies for the substep.
        if (targetStarts != null)
        {
            foreach (var (target, _) in targetStarts)
            {
                if (!TryGetTargetDisplacement(target, out var targetDisplacement) ||
                    projectile.ProcessedTargets.Contains(target))
                {
                    continue;
                }

                float MovingTargetCallback(
                    FixtureProxy proxy,
                    Vector2 point,
                    Vector2 normal,
                    float fraction,
                    ref RayResult movingResult)
                {
                    if (proxy.Entity != target ||
                        !CanFixturesCollide(
                            uid,
                            projectileBody,
                            projectileFixture,
                            proxy.Entity,
                            proxy.Body,
                            proxy.Fixture))
                    {
                        return -1f;
                    }

                    return RayCastSystem.RayCastAllCallback(
                        proxy,
                        point,
                        normal,
                        fraction,
                        ref movingResult);
                }

                var relativeResult = _rayCast.CastShape(
                    start.MapId,
                    castShape,
                    new Robust.Shared.Physics.Transform(start.Position + targetDisplacement, projectileAngle),
                    translation - targetDisplacement,
                    filter,
                    MovingTargetCallback);

                foreach (var relativeHit in relativeResult.Results)
                {
                    var correctedHit = relativeHit;
                    correctedHit.Point -= targetDisplacement * (1f - correctedHit.Fraction);
                    hits.Add(correctedHit);
                }
            }
        }

        if (hits.Count == 0)
            return;

        hits.Sort(static (a, b) => a.Fraction.CompareTo(b.Fraction));
        var originalVelocity = _physics.GetLinearVelocity(uid, projectileBody.LocalCenter, projectileBody);

        foreach (var hit in hits)
        {
            if (projectile.ProjectileSpent || TerminatingOrDeleted(uid))
                break;

            if (projectile.ProcessedTargets.Contains(hit.Entity))
                continue;

            var projectilePosition = start.Position + translation * hit.Fraction;
            _transform.SetMapCoordinates(
                (uid, xform),
                new MapCoordinates(projectilePosition, start.MapId));

            ProjectileCollide(
                (uid, projectile, projectileBody),
                hit.Entity,
                collisionCoordinates: new MapCoordinates(hit.Point, start.MapId));

            if (projectile.ProjectileSpent || TerminatingOrDeleted(uid))
                return;

            // Reflections change the velocity during ProjectileReflectAttemptEvent. Stop using the old path.
            var currentVelocity = _physics.GetLinearVelocity(uid, projectileBody.LocalCenter, projectileBody);
            if (!currentVelocity.EqualsApprox(originalVelocity))
                return;
        }

        if (!projectile.ProjectileSpent && !TerminatingOrDeleted(uid))
            _transform.SetMapCoordinates((uid, xform), end);
    }

    private bool CanFixturesCollide(
        EntityUid projectile,
        PhysicsComponent projectileBody,
        Fixture projectileFixture,
        EntityUid target,
        PhysicsComponent targetBody,
        Fixture targetFixture)
    {
        if (!targetFixture.Hard ||
            ((targetFixture.CollisionLayer & projectileFixture.CollisionMask) == 0 &&
             (targetFixture.CollisionMask & projectileFixture.CollisionLayer) == 0))
        {
            return false;
        }

        var projectileEvent = new PreventCollideEvent(
            projectile,
            target,
            projectileBody,
            targetBody,
            projectileFixture,
            targetFixture);
        RaiseLocalEvent(projectile, ref projectileEvent);
        if (projectileEvent.Cancelled)
            return false;

        var targetEvent = new PreventCollideEvent(
            target,
            projectile,
            targetBody,
            projectileBody,
            targetFixture,
            projectileFixture);
        RaiseLocalEvent(target, ref targetEvent);
        return !targetEvent.Cancelled;
    }

    private void OnStartCollide(EntityUid uid, ProjectileComponent component, ref StartCollideEvent args)
    {
        // This is so entities that shouldn't get a collision are ignored.
        if (args.OurFixtureId != ProjectileFixture || !args.OtherFixture.Hard
            || component.ProjectileSpent || component is { Weapon: null, OnlyCollideWhenShot: true })
            return;

        if (component.ProcessedTargets.Contains(args.OtherEntity))
            return;

        MapCoordinates? collisionCoordinates = null;
        if (args.PointCount > 0)
        {
            var points = args.WorldPoints;
            var contact = Vector2.Zero;
            foreach (var point in points)
                contact += point;

            collisionCoordinates = new MapCoordinates(
                contact / points.Length,
                _transform.GetMapCoordinates(uid).MapId);
        }

        ProjectileCollide(
            (uid, component, args.OurBody),
            args.OtherEntity,
            collisionCoordinates: collisionCoordinates);
    }

    public void ProjectileCollide(
        Entity<ProjectileComponent, PhysicsComponent> projectile,
        EntityUid target,
        bool predicted = false,
        MapCoordinates? collisionCoordinates = null,
        bool suppressPredictedShooterEffects = false)
    {
        var (uid, component, body) = projectile;
        if (component.ProjectileSpent || component.ProcessedTargets.Contains(target))
            return;

        // DS14-start
        // Filter.Pvs ignores session view subscriptions used by remote eyes.
        var effectOrigin = _transform.GetMapCoordinates(target);
        var effectFilter = Filter.Empty().AddPlayersByPvs(effectOrigin, entManager: EntityManager)
            .AddPlayersByViewSubscriptions(effectOrigin, entityManager: EntityManager);
        // DS14-end
        if (suppressPredictedShooterEffects &&
            TryComp<PredictedProjectileComponent>(uid, out var predictedProjectile) &&
            TryComp<ActorComponent>(predictedProjectile.Shooter, out var predictedActor))
        {
            effectFilter.RemovePlayer(predictedActor.PlayerSession);
        }

        // it's here so this check is only done once before possible hit
        var attemptEv = new ProjectileReflectAttemptEvent(uid, component, false);
        RaiseLocalEvent(target, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            SetShooter(uid, component, target);
            if (predicted)
                ReconcilePredictedProjectile(uid);
            return;
        }

        if (!component.ProcessedTargets.Add(target))
            return;

        var ev = new ProjectileHitEvent(component.Damage * _damageableSystem.UniversalProjectileDamageModifier, target, component.Shooter);
        RaiseLocalEvent(uid, ref ev);

        var otherName = ToPrettyString(target);
        var damageRequired = _destructibleSystem.DestroyedAt(target);
        if (TryComp<DamageableComponent>(target, out var damageableComponent))
        {
            damageRequired -= damageableComponent.TotalDamage;
            damageRequired = FixedPoint2.Max(damageRequired, FixedPoint2.Zero);
        }
        var deleted = Deleted(target);

        if (_damageableSystem.TryChangeDamage((target, damageableComponent), ev.Damage, out var damage, component.IgnoreResistances, origin: component.Shooter) && Exists(component.Shooter))
        {
            if (!deleted)
            {
                _color.RaiseEffect(Color.Red, new List<EntityUid> { target }, effectFilter);
            }

            _adminLogger.Add(LogType.BulletHit,
                LogImpact.Medium,
                $"Projectile {ToPrettyString(uid):projectile} shot by {ToPrettyString(component.Shooter!.Value):user} hit {otherName:target} and dealt {damage:damage} damage");

            component.ProjectileSpent = !TryPenetrate((uid, component), damage, damageRequired);
        }
        else
        {
            component.ProjectileSpent = true;
        }

        if (!deleted)
        {
            _guns.PlayImpactSound(target, damage, component.SoundHit, component.ForceSound, effectFilter);

            if (!body.LinearVelocity.IsLengthZero())
                _sharedCameraRecoil.KickCamera(target, body.LinearVelocity.Normalized());
        }

        if (!predicted && component.DeleteOnCollide && component.ProjectileSpent)
            QueueDel(uid);

        if (predicted && component.DeleteOnCollide && component.ProjectileSpent)
        {
            var predictedHit = EnsureComp<PredictedProjectileHitComponent>(uid);
            var origin = TryComp<PredictedProjectileComponent>(uid, out var prediction) &&
                         prediction.Origin.MapId != MapId.Nullspace
                ? prediction.Origin
                : _transform.GetMapCoordinates(uid);
            predictedHit.Origin = _transform.GetMoverCoordinates(_transform.ToCoordinates(origin));

            var targetCoordinates = _transform.GetMoverCoordinates(
                _transform.ToCoordinates(collisionCoordinates ?? _transform.GetMapCoordinates(target)));
            if (predictedHit.Origin.TryDistance(EntityManager, _transform, targetCoordinates, out var distance))
                predictedHit.Distance = distance;

            Dirty(uid, predictedHit);
        }
        else if (predicted && (!component.ProjectileSpent || !component.DeleteOnCollide))
        {
            ReconcilePredictedProjectile(uid);
        }

        if (component.ImpactEffect != null && TryComp(uid, out TransformComponent? xform))
        {
            var effectCoordinates = collisionCoordinates is { } contact
                ? _transform.ToCoordinates(contact)
                : xform.Coordinates;
            RaiseNetworkEvent(
                new ImpactEffectEvent(component.ImpactEffect, GetNetCoordinates(effectCoordinates)),
                effectFilter);
        }
    }

    public void ReconcilePredictedProjectile(EntityUid uid)
    {
        if (!TryComp(uid, out PredictedProjectileComponent? predicted) || predicted.Reconciled)
            return;

        predicted.Reconciled = true;

        if (TryComp<ActorComponent>(predicted.Shooter, out var actor))
        {
            RaiseNetworkEvent(
                new PredictedProjectileReconcileEvent(predicted.PredictionId, predicted.ProjectileIndex),
                Filter.SinglePlayer(actor.PlayerSession));
        }
    }

    private bool TryPenetrate(Entity<ProjectileComponent> projectile, DamageSpecifier damage, FixedPoint2 damageRequired)
    {
        // If penetration is to be considered, we need to do some checks to see if the projectile should stop.
        if (projectile.Comp.PenetrationThreshold == 0)
            return false;

        // If a damage type is required, stop the bullet if the hit entity doesn't have that type.
        if (projectile.Comp.PenetrationDamageTypeRequirement != null)
        {
            foreach (var requiredDamageType in projectile.Comp.PenetrationDamageTypeRequirement)
            {
                if (damage.DamageDict.Keys.Contains(requiredDamageType))
                    continue;

                return false;
            }
        }

        // If the object won't be destroyed, it "tanks" the penetration hit.
        if (damage.GetTotal() < damageRequired)
        {
            return false;
        }

        if (!projectile.Comp.ProjectileSpent)
        {
            projectile.Comp.PenetrationAmount += damageRequired;
            // The projectile has dealt enough damage to be spent.
            if (projectile.Comp.PenetrationAmount >= projectile.Comp.PenetrationThreshold)
            {
                return false;
            }
        }

        return true;
    }
}
