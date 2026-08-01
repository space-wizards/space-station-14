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
using Robust.Shared.Physics.Components;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Projectiles;

public sealed class ProjectileSystem : SharedProjectileSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ColorFlashEffectSystem _color = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly DestructibleSystem _destructibleSystem = default!;
    [Dependency] private readonly GunSystem _guns = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(EntityUid uid, ProjectileComponent component, ref StartCollideEvent args)
    {
        // This is so entities that shouldn't get a collision are ignored.
        if (args.OurFixtureId != ProjectileFixture || !args.OtherFixture.Hard
            || component.ProjectileSpent || component is { Weapon: null, OnlyCollideWhenShot: true })
            return;

        if (TryComp(uid, out PredictedProjectileComponent? predicted) &&
            predicted.ProcessedTargets.Contains(args.OtherEntity))
        {
            return;
        }

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
        bool suppressPredictedShooterEffects = true)
    {
        var (uid, component, body) = projectile;
        if (component.ProjectileSpent)
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
