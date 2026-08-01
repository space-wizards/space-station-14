using System.Numerics;
using System.Linq;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DeadSpace.CCCCVars;
using Content.Shared.Effects;
using Content.Shared.Projectiles;
using Content.Shared.Sound.Components;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.GameObjects;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;

namespace Content.Client.Projectiles;

public sealed partial class ProjectileSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly RayCastSystem _rayCast = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RequireProjectileTargetSystem _requireProjectileTarget = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly Dictionary<(uint Id, ushort Index), EntityUid> _predictedProjectiles = new();
    private readonly Dictionary<(uint Id, ushort Index), TimeSpan> _reconciledProjectiles = new();
    private readonly HashSet<EntityUid> _sweepCandidates = new();
    private const float PredictionLifetime = 30f;
    private const float AuthoritativeProjectileTimeout = 2f;
    private const float RejectedPredictionHitLifetime = 1f;
    private const float DamagePitchVariation = 0.05f;
    private const float MovingTargetLookupSpeed = 80f;

    public bool PredictionEnabled => _cfg.GetCVar(CCCCVars.ProjectilePredictionEnabled);

    private void InitializePrediction()
    {
        SubscribeLocalEvent<PredictedProjectileVisualComponent, UpdateIsPredictedEvent>(OnUpdateIsPredicted);
        SubscribeLocalEvent<PredictedProjectileVisualComponent, StartCollideEvent>(OnPredictedStartCollide);
        SubscribeLocalEvent<PredictedProjectileVisualComponent, EntityTerminatingEvent>(OnPredictedTerminating);
        SubscribeLocalEvent<PredictedProjectileComponent, ComponentStartup>(OnAuthoritativeStartup);
        SubscribeLocalEvent<PredictedProjectileComponent, EntityTerminatingEvent>(OnAuthoritativeTerminating);
        SubscribeNetworkEvent<PredictedProjectileReconcileEvent>(OnReconcilePredictedProjectile);
        SubscribeLocalEvent<PhysicsUpdateBeforeSolveEvent>(OnBeforeSolve);
        SubscribeLocalEvent<PhysicsUpdateAfterSolveEvent>(OnAfterSolve);

        UpdatesBefore.Add(typeof(TransformSystem));
    }

    public void RegisterPredictedProjectile(EntityUid uid, uint predictionId, ushort projectileIndex)
    {
        if (!PredictionEnabled || predictionId == 0 || !IsClientSide(uid))
            return;

        var key = (predictionId, projectileIndex);
        if (_predictedProjectiles.TryGetValue(key, out var old) && old != uid && Exists(old))
            QueueDel(old);

        var predicted = EnsureComp<PredictedProjectileVisualComponent>(uid);
        predicted.PredictionId = predictionId;
        predicted.ProjectileIndex = projectileIndex;
        predicted.Origin = _transform.GetMapCoordinates(uid);
        predicted.CreatedAt = _timing.CurTime;
        _predictedProjectiles[key] = uid;

        // This entity is a moving visual proxy. Its lifetime and collisions are reconciled here;
        // gameplay triggers must only run on the authoritative server projectile.
        RemComp<TimedDespawnComponent>(uid);
        RemComp<TimerTriggerComponent>(uid);
        RemComp<RandomTimerTriggerComponent>(uid);
        RemComp<ActiveTimerTriggerComponent>(uid);
        RemComp<TriggerOnCollideComponent>(uid);
        RemComp<TriggerOnTimedCollideComponent>(uid);
        RemComp<ActiveTriggerOnTimedCollideComponent>(uid);

        if (HasComp<TriggerOnProximityComponent>(uid) && TryComp(uid, out PhysicsComponent? body))
            _fixtures.DestroyFixture(uid, TriggerOnProximityComponent.FixtureID, body: body);

        RemComp<TriggerOnProximityComponent>(uid);
        RemComp<DamageContactsComponent>(uid);
        RemComp<DamageOnHighSpeedImpactComponent>(uid);
        RemComp<EmitSoundOnCollideComponent>(uid);

        _physics.UpdateIsPredicted(uid);
        Transform(uid).ActivelyLerping = false;
    }

    private void OnUpdateIsPredicted(Entity<PredictedProjectileVisualComponent> ent, ref UpdateIsPredictedEvent args)
    {
        args.IsPredicted = true;
    }

    private void OnBeforeSolve(ref PhysicsUpdateBeforeSolveEvent args)
    {
        var query = EntityQueryEnumerator<PredictedProjectileVisualComponent>();
        while (query.MoveNext(out var uid, out var predicted))
        {
            if (TerminatingOrDeleted(uid))
                continue;

            predicted.CoordinatesBeforePredictionReplay = Transform(uid).Coordinates;
            if (_timing.IsFirstTimePredicted && predicted.HitAt == null)
            {
                predicted.CoordinatesBeforePhysics = _transform.GetMapCoordinates(uid);
                CaptureMovingTargetPositions((uid, predicted), args.DeltaTime);
            }
        }
    }

    private void OnAfterSolve(ref PhysicsUpdateAfterSolveEvent args)
    {
        if (_timing.IsFirstTimePredicted)
        {
            var sweepQuery = EntityQueryEnumerator<PredictedProjectileVisualComponent>();
            while (sweepQuery.MoveNext(out var uid, out var predicted))
            {
                var start = predicted.CoordinatesBeforePhysics;
                predicted.CoordinatesBeforePhysics = null;
                if (start != null && predicted.HitAt == null && !TerminatingOrDeleted(uid))
                    SweepPredictedProjectile((uid, predicted), start.Value);

                predicted.TargetCoordinatesBeforePhysics.Clear();
            }

            return;
        }

        var query = EntityQueryEnumerator<PredictedProjectileVisualComponent>();
        while (query.MoveNext(out var uid, out var predicted))
        {
            if (!TerminatingOrDeleted(uid) && predicted.CoordinatesBeforePredictionReplay is { } coordinates)
                _transform.SetCoordinates(uid, coordinates);

            predicted.CoordinatesBeforePredictionReplay = null;
            predicted.CoordinatesBeforePhysics = null;
        }
    }

    private void CaptureMovingTargetPositions(
        Entity<PredictedProjectileVisualComponent> ent,
        float deltaTime)
    {
        ent.Comp.TargetCoordinatesBeforePhysics.Clear();
        if (!TryComp(ent, out PhysicsComponent? projectileBody) ||
            !TryComp(ent, out FixturesComponent? fixtures) ||
            !fixtures.Fixtures.TryGetValue(SharedProjectileSystem.ProjectileFixture, out var projectileFixture) ||
            !TryComp(ent, out TransformComponent? xform))
        {
            return;
        }

        var start = _transform.GetMapCoordinates(ent, xform);
        if (start.MapId == MapId.Nullspace)
            return;

        var expectedTranslation = _physics.GetMapLinearVelocity(ent, projectileBody, xform) * deltaTime;
        if (expectedTranslation.LengthSquared() < 0.000001f)
            return;

        var castShape = GetProjectileCastShape(projectileFixture.Shape);
        var projectileAngle = projectileFixture.Shape is PhysShapeAabb
            ? Angle.Zero
            : _transform.GetWorldRotation(xform);
        var startTransform = new Robust.Shared.Physics.Transform(start.Position, projectileAngle);
        var endTransform = new Robust.Shared.Physics.Transform(start.Position + expectedTranslation, projectileAngle);
        var lookupBounds = castShape.ComputeAABB(startTransform, 0)
            .Union(castShape.ComputeAABB(endTransform, 0))
            .Enlarged(MathF.Max(0.5f, MovingTargetLookupSpeed * deltaTime));

        _sweepCandidates.Clear();
        _entityLookup.GetEntitiesIntersecting(
            start.MapId,
            lookupBounds,
            _sweepCandidates,
            LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Approximate);

        foreach (var target in _sweepCandidates)
        {
            if (target == ent.Owner ||
                !TryComp(target, out FixturesComponent? targetFixtures) ||
                !HasComp<PhysicsComponent>(target) ||
                !TryComp(target, out TransformComponent? targetXform))
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
            if (targetCoordinates.MapId == start.MapId)
                ent.Comp.TargetCoordinatesBeforePhysics[target] = targetCoordinates;
        }
    }

    /// <summary>
    /// Client physics has no continuous collision solver, so a fast predicted projectile can move from
    /// one side of a target to the other without producing StartCollideEvent. Cast its fixture over the
    /// complete substep and report the first valid contact immediately.
    /// </summary>
    private void SweepPredictedProjectile(
        Entity<PredictedProjectileVisualComponent> ent,
        MapCoordinates start)
    {
        if (!TryComp(ent, out ProjectileComponent? projectile) ||
            projectile.ProjectileSpent ||
            !TryComp(ent, out PhysicsComponent? projectileBody) ||
            !TryComp(ent, out FixturesComponent? fixtures) ||
            !fixtures.Fixtures.TryGetValue(SharedProjectileSystem.ProjectileFixture, out var projectileFixture) ||
            !TryComp(ent, out TransformComponent? xform))
        {
            return;
        }

        var end = _transform.GetMapCoordinates(ent, xform);
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
            if (!ent.Comp.TargetCoordinatesBeforePhysics.TryGetValue(target, out var targetStart) ||
                !TryComp(target, out TransformComponent? targetXform))
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
            if (proxy.Entity == ent.Owner ||
                TryGetTargetDisplacement(proxy.Entity, out _) ||
                !CanPredictHit(ent, proxy.Entity) ||
                !CanFixturesCollide(
                    ent,
                    projectileBody,
                    projectileFixture,
                    proxy.Entity,
                    proxy.Body,
                    proxy.Fixture))
            {
                return -1f;
            }

            return RayCastSystem.RayCastClosestCallback(proxy, point, normal, fraction, ref result);
        }

        var result = _rayCast.CastShape(
            start.MapId,
            castShape,
            new Robust.Shared.Physics.Transform(start.Position, projectileAngle),
            translation,
            filter,
            SweepCallback);
        var hits = new List<RayHit>(result.Results.Count + ent.Comp.TargetCoordinatesBeforePhysics.Count);
        foreach (var sweepHit in result.Results)
            hits.Add(sweepHit);

        // YAML AABBs become polygons with a skin radius and Robust sends those through its known-broken
        // GJK ray-vs-box path. Test every captured polygon blocker with swept SAT in its relative frame.
        foreach (var (target, targetStart) in ent.Comp.TargetCoordinatesBeforePhysics)
        {
            if (!CanPredictHit(ent, target) ||
                !TryComp(target, out PhysicsComponent? targetBody) ||
                !TryComp(target, out FixturesComponent? targetFixtures) ||
                !TryComp(target, out TransformComponent? targetXform))
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
                        ent,
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

        foreach (var (target, _) in ent.Comp.TargetCoordinatesBeforePhysics)
        {
            if (!TryGetTargetDisplacement(target, out var targetDisplacement) ||
                !CanPredictHit(ent, target))
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
                        ent,
                        projectileBody,
                        projectileFixture,
                        proxy.Entity,
                        proxy.Body,
                        proxy.Fixture))
                {
                    return -1f;
                }

                return RayCastSystem.RayCastClosestCallback(
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

        if (hits.Count == 0)
            return;

        hits.Sort(static (a, b) => a.Fraction.CompareTo(b.Fraction));
        var hit = hits[0];
        var projectileCoordinates = new MapCoordinates(
            start.Position + translation * hit.Fraction,
            start.MapId);
        _transform.SetMapCoordinates((ent.Owner, xform), projectileCoordinates);
        ProcessPredictedCollision(
            ent,
            hit.Entity,
            projectileCoordinates,
            new MapCoordinates(hit.Point, start.MapId));
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

    private void OnPredictedStartCollide(Entity<PredictedProjectileVisualComponent> ent, ref StartCollideEvent args)
    {
        if (ent.Comp.HitAt != null ||
            args.OurFixtureId != SharedProjectileSystem.ProjectileFixture ||
            !args.OtherFixture.Hard ||
            !CanPredictHit(ent, args.OtherEntity))
        {
            return;
        }

        var projectileCoordinates = _transform.GetMapCoordinates(ent);
        var contactCoordinates = GetContactCoordinates(args, projectileCoordinates);

        if (!_timing.IsFirstTimePredicted)
        {
            if (ent.Comp.PendingCollision == null)
            {
                ent.Comp.PendingCollision = args.OtherEntity;
                ent.Comp.PendingProjectileCoordinates = projectileCoordinates;
                ent.Comp.PendingContactCoordinates = contactCoordinates;
            }
            return;
        }

        ProcessPredictedCollision(ent, args.OtherEntity, projectileCoordinates, contactCoordinates);
    }

    private MapCoordinates GetContactCoordinates(StartCollideEvent args, MapCoordinates fallback)
    {
        if (args.PointCount == 0)
            return fallback;

        var points = args.WorldPoints;
        var contact = Vector2.Zero;
        foreach (var point in points)
            contact += point;

        return new MapCoordinates(contact / points.Length, fallback.MapId);
    }

    private void ProcessPredictedCollision(
        Entity<PredictedProjectileVisualComponent> ent,
        EntityUid target,
        MapCoordinates projectileCoordinates,
        MapCoordinates contactCoordinates)
    {
        if (ent.Comp.HitAt != null || !CanPredictHit(ent, target))
            return;

        if (!TryComp(ent, out ProjectileComponent? projectile))
            return;

        SendPredictedHit(ent, target, projectileCoordinates, contactCoordinates);

        var hit = new ProjectileHitEvent(projectile.Damage, target, projectile.Shooter);
        RaiseLocalEvent(ent, ref hit);

        ReportPredictedHit(
            ent,
            new HashSet<EntityUid> { target },
            projectileCoordinates,
            contactCoordinates);
    }

    private void SendPredictedHit(
        Entity<PredictedProjectileVisualComponent> ent,
        EntityUid target,
        MapCoordinates projectileCoordinates,
        MapCoordinates contactCoordinates)
    {
        RaiseNetworkEvent(new PredictedProjectileHitEvent(
            ent.Comp.PredictionId,
            ent.Comp.ProjectileIndex,
            new HashSet<(NetEntity, MapCoordinates, MapCoordinates, MapCoordinates)>
            {
                (
                    GetNetEntity(target),
                    _transform.GetMapCoordinates(target),
                    projectileCoordinates,
                    contactCoordinates),
            }));
    }

    private void ReportPredictedHit(
        Entity<PredictedProjectileVisualComponent> ent,
        HashSet<EntityUid> targets,
        MapCoordinates projectileCoordinates,
        MapCoordinates contactCoordinates)
    {
        if (ent.Comp.HitAt != null || targets.Count == 0)
            return;

        ent.Comp.HitAt = _timing.CurTime;
        if (projectileCoordinates.MapId == ent.Comp.Origin.MapId)
            ent.Comp.HitDistance = (projectileCoordinates.Position - ent.Comp.Origin.Position).Length();

        PlayPredictedImpact(ent, _transform.ToCoordinates(contactCoordinates));

        if (TryComp(ent, out ProjectileComponent? projectile))
        {
            PlayPredictedImpactSound(projectile, targets.First());
            projectile.ProjectileSpent = true;
        }

        if (projectile != null && projectile.Damage.AnyPositive())
            _color.RaiseEffect(Color.Red, new List<EntityUid> { targets.First() }, Filter.Local());

        SetVisuals(ent, false);
        if (TryComp(ent, out PhysicsComponent? physics))
            _physics.SetLinearVelocity(ent, Vector2.Zero, body: physics);
    }

    private void PlayPredictedImpactSound(ProjectileComponent projectile, EntityUid target)
    {
        SoundSpecifier? sound = projectile.SoundHit;
        if (!projectile.ForceSound &&
            projectile.Damage.AnyPositive() &&
            TryComp(target, out RangedDamageSoundComponent? rangedSound))
        {
            var type = Content.Shared.Weapons.Melee.SharedMeleeWeaponSystem.GetHighestDamageSound(
                projectile.Damage,
                _prototypes);

            if (type != null && rangedSound.SoundTypes?.TryGetValue(type, out var typedSound) == true)
                sound = typedSound;
            else if (type != null && rangedSound.SoundGroups?.TryGetValue(type, out var groupedSound) == true)
                sound = groupedSound;
        }

        if (sound != null)
            _audio.PlayPredicted(sound, target, _players.LocalEntity,
                AudioParams.Default.WithVariation(DamagePitchVariation));
    }

    private bool CanPredictHit(EntityUid projectile, EntityUid target)
    {
        if (IsClientSide(target))
            return false;

        if (!TryComp(target, out RequireProjectileTargetComponent? requireTarget) ||
            !_requireProjectileTarget.RequiresExplicitTarget((target, requireTarget)))
        {
            return true;
        }

        return TryComp(projectile, out TargetedProjectileComponent? targeted) && targeted.Target == target;
    }

    private void OnPredictedTerminating(Entity<PredictedProjectileVisualComponent> ent, ref EntityTerminatingEvent args)
    {
        var key = (ent.Comp.PredictionId, ent.Comp.ProjectileIndex);
        if (_predictedProjectiles.TryGetValue(key, out var current) && current == ent.Owner)
            _predictedProjectiles.Remove(key);
    }

    private void OnAuthoritativeStartup(Entity<PredictedProjectileComponent> ent, ref ComponentStartup args)
    {
        if (!PredictionEnabled || ent.Comp.Shooter != _players.LocalEntity || ent.Comp.PredictionId == 0)
            return;

        var key = (ent.Comp.PredictionId, ent.Comp.ProjectileIndex);
        if (_reconciledProjectiles.Remove(key))
        {
            if (_predictedProjectiles.TryGetValue(key, out var reconciledPredicted) && Exists(reconciledPredicted))
                QueueDel(reconciledPredicted);
            return;
        }

        // The shooter already has a local projectile for this shot. Always hide the server copy,
        // even if the local copy collided or despawned before this component reached the client.
        HideAuthoritative(ent);

        if (!_predictedProjectiles.TryGetValue(key, out var predictedUid) ||
            !TryComp(predictedUid, out PredictedProjectileVisualComponent? predicted))
        {
            return;
        }

        predicted.AuthoritativeProjectile = ent;
    }

    private void OnReconcilePredictedProjectile(PredictedProjectileReconcileEvent ev)
    {
        var key = (ev.PredictionId, ev.ProjectileIndex);
        _reconciledProjectiles[key] = _timing.CurTime;
        if (_predictedProjectiles.TryGetValue(key, out var predicted) && Exists(predicted))
            QueueDel(predicted);

        var query = EntityQueryEnumerator<PredictedProjectileComponent>();
        while (query.MoveNext(out var uid, out var authoritative))
        {
            if (authoritative.Shooter != _players.LocalEntity ||
                authoritative.PredictionId != ev.PredictionId ||
                authoritative.ProjectileIndex != ev.ProjectileIndex)
            {
                continue;
            }

            RevealAuthoritative(uid);
            _reconciledProjectiles.Remove(key);
            break;
        }
    }

    private void OnAuthoritativeTerminating(Entity<PredictedProjectileComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Shooter != _players.LocalEntity || ent.Comp.PredictionId == 0)
            return;

        var key = (ent.Comp.PredictionId, ent.Comp.ProjectileIndex);
        if (_predictedProjectiles.TryGetValue(key, out var predicted) && Exists(predicted))
            QueueDel(predicted);
    }

    private void HideAuthoritative(EntityUid uid)
    {
        if (HasComp<HiddenPredictedProjectileComponent>(uid))
            return;

        var hidden = EnsureComp<HiddenPredictedProjectileComponent>(uid);
        if (TryComp(uid, out SpriteComponent? sprite))
        {
            hidden.SpriteVisible = sprite.Visible;
            _sprite.SetVisible((uid, sprite), false);
        }

        if (TryComp(uid, out PointLightComponent? light))
        {
            hidden.LightEnabled = light.Enabled;
            _lights.SetEnabled(uid, false, light);
        }
    }

    private void RevealAuthoritative(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid) || !TryComp(uid, out HiddenPredictedProjectileComponent? hidden))
            return;

        if (TryComp(uid, out SpriteComponent? sprite))
            _sprite.SetVisible((uid, sprite), hidden.SpriteVisible);

        if (TryComp(uid, out PointLightComponent? light))
            _lights.SetEnabled(uid, hidden.LightEnabled, light);

        RemCompDeferred<HiddenPredictedProjectileComponent>(uid);
    }

    private void SetVisuals(EntityUid uid, bool visible)
    {
        if (TryComp(uid, out SpriteComponent? sprite))
            _sprite.SetVisible((uid, sprite), visible);

        if (TryComp(uid, out PointLightComponent? light))
            _lights.SetEnabled(uid, visible, light);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.IsFirstTimePredicted)
        {
            var contactQuery = EntityQueryEnumerator<PredictedProjectileVisualComponent, FixturesComponent>();
            while (contactQuery.MoveNext(out var uid, out var predicted, out var fixtures))
            {
                if (predicted.HitAt != null)
                    continue;

                var pending = predicted.PendingCollision;
                predicted.PendingCollision = null;
                if (pending is { } pendingTarget &&
                    !TerminatingOrDeleted(pendingTarget) &&
                    CanPredictHit(uid, pendingTarget))
                {
                    ProcessPredictedCollision(
                        (uid, predicted),
                        pendingTarget,
                        predicted.PendingProjectileCoordinates,
                        predicted.PendingContactCoordinates);
                    continue;
                }

                if (TryGetPredictedContact(uid, fixtures, out var target))
                {
                    var projectileCoordinates = _transform.GetMapCoordinates(uid);
                    ProcessPredictedCollision(
                        (uid, predicted),
                        target,
                        projectileCoordinates,
                        projectileCoordinates);
                }
            }
        }

        var now = _timing.CurTime;
        foreach (var reconciled in _reconciledProjectiles.ToArray())
        {
            if (now - reconciled.Value > TimeSpan.FromSeconds(PredictionLifetime))
                _reconciledProjectiles.Remove(reconciled.Key);
        }

        var query = EntityQueryEnumerator<PredictedProjectileVisualComponent>();
        while (query.MoveNext(out var uid, out var predicted))
        {
            if (TerminatingOrDeleted(uid))
                continue;

            if (now - predicted.CreatedAt > TimeSpan.FromSeconds(PredictionLifetime))
            {
                if (predicted.AuthoritativeProjectile is { } staleAuthoritative && Exists(staleAuthoritative))
                    RevealAuthoritative(staleAuthoritative);

                QueueDel(uid);
                continue;
            }

            if (predicted.AuthoritativeProjectile is not { } authoritative ||
                TerminatingOrDeleted(authoritative))
            {
                if (predicted.HitAt is { } hitAt &&
                    now - hitAt > TimeSpan.FromSeconds(RejectedPredictionHitLifetime))
                {
                    QueueDel(uid);
                }
                else if (now - predicted.CreatedAt > TimeSpan.FromSeconds(AuthoritativeProjectileTimeout))
                {
                    QueueDel(uid);
                }

                continue;
            }

            if (predicted.HitAt == null || predicted.HitDistance is not { } hitDistance)
                continue;

            var authoritativePosition = _transform.GetMapCoordinates(authoritative);
            if (authoritativePosition.MapId != predicted.Origin.MapId)
                continue;

            var authoritativeDistance = (authoritativePosition.Position - predicted.Origin.Position).Length();
            if (authoritativeDistance + 0.25f < hitDistance)
                continue;

            predicted.AuthoritativeProjectile = null;
            QueueDel(uid);
        }
    }

    private bool TryGetPredictedContact(
        EntityUid projectile,
        FixturesComponent fixtures,
        out EntityUid target)
    {
        if (!fixtures.Fixtures.TryGetValue(SharedProjectileSystem.ProjectileFixture, out var projectileFixture))
        {
            target = default;
            return false;
        }

        foreach (var contact in projectileFixture.Contacts.Values)
        {
            if (contact.Deleting || !contact.Enabled || !contact.IsTouching)
                continue;

            var projectileIsA = contact.EntityA == projectile;
            var targetFixture = projectileIsA ? contact.FixtureB : contact.FixtureA;
            var candidate = projectileIsA ? contact.EntityB : contact.EntityA;

            if (targetFixture?.Hard != true ||
                !CanPredictHit(projectile, candidate))
            {
                continue;
            }

            target = candidate;
            return true;
        }

        target = default;
        return false;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<PredictedProjectileVisualComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (!TerminatingOrDeleted(uid))
                xform.ActivelyLerping = false;
        }
    }
}
