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
using Robust.Shared.Physics.Components;
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
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly ProjectileSystem _projectiles = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RequireProjectileTargetSystem _requireProjectileTarget = default!;

    private readonly Dictionary<(NetUserId User, uint Id, ushort Index), EntityUid> _predicted = new();
    private readonly List<(PredictedProjectileHitEvent Event, ICommonSession Player)> _predictedHits = new();

    private bool _preventCollision;
    private bool _logHits;
    private float _coordinateDeviation;
    private float _lowestCoordinateDeviation;
    private float _aabbEnlargement;

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
        if (!_preventCollision || args.Cancelled)
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

        predicted.Hit = true;
        var accepted = false;
        foreach (var (netEntity, clientCoordinates) in ev.Hits)
        {
            var target = GetEntity(netEntity);
            if (!target.Valid ||
                !_lagCompensationQuery.TryComp(target, out var lagCompensation) ||
                !_fixturesQuery.TryComp(target, out var fixtures) ||
                !_physicsQuery.TryComp(target, out var physics) ||
                !_transformQuery.TryComp(target, out var xform))
            {
                continue;
            }

            if (projectileComponent.IgnoreShooter &&
                (target == projectileComponent.Shooter || target == projectileComponent.Weapon))
            {
                continue;
            }

            if (TryComp(target, out RequireProjectileTargetComponent? requireTarget) &&
                _requireProjectileTarget.RequiresExplicitTarget((target, requireTarget)) &&
                (!TryComp(projectile, out TargetedProjectileComponent? targeted) || targeted.Target != target))
            {
                continue;
            }

            if (!Collides(
                    (projectile, predicted, projectilePhysics),
                    (target, lagCompensation, fixtures, physics, xform),
                    clientCoordinates))
            {
                if (_logHits)
                    Log.Info($"Rejected predicted hit from {player.Name} on {ToPrettyString(target)}");
                continue;
            }

            if (_logHits)
                Log.Info($"Accepted predicted hit from {player.Name} on {ToPrettyString(target)}");

            predicted.ProcessedTargets.Add(target);
            _projectiles.ProjectileCollide((projectile, projectileComponent, projectilePhysics), target, true);
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
