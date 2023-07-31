using Content.Server.Administration.Logs;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons;
using Content.Shared.Weapons.Ranged.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Physics.Events;

namespace Content.Server.Projectiles;

public sealed class ProjectileSystem : SharedProjectileSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly GunSystem _guns = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(EntityUid uid, ProjectileComponent component, ref StartCollideEvent args)
    {
        // This is so entities that shouldn't get a collision are ignored.
        if (args.OurFixture.ID != ProjectileFixture || !args.OtherFixture.Hard || component.DamagedEntity)
            return;

        var otherEntity = args.OtherEntity;
        // it's here so this check is only done once before possible hit
        var attemptEv = new ProjectileReflectAttemptEvent(uid, component, false);
        RaiseLocalEvent(otherEntity, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            SetShooter(component, otherEntity);
            return;
        }

        if (TryComp<GunComponent>(component.Weapon, out var gun) && !component.DamageModifierAdded)
        {
            component.Damage *= gun.DamageMultiplier;
            component.DamageModifierAdded = true;
        }

        float? resistancePenetration = null;
        if (TryComp<ResistancePenetrationComponent>(uid, out var penComp))
            resistancePenetration = penComp.Penetration;

        var otherName = ToPrettyString(otherEntity);
        var direction = args.OurBody.LinearVelocity.Normalized();
        var modifiedDamage = _damageableSystem.TryChangeDamage(otherEntity, component.Damage, resistancePenetration, origin: component.Shooter);
        var deleted = Deleted(otherEntity);

        if (modifiedDamage is not null && EntityManager.EntityExists(component.Shooter))
        {
            if (modifiedDamage.Total > FixedPoint2.Zero && !deleted)
            {
                RaiseNetworkEvent(new DamageEffectEvent(Color.Red, new List<EntityUid> {otherEntity}), Filter.Pvs(otherEntity, entityManager: EntityManager));
            }

            _adminLogger.Add(LogType.BulletHit,
                HasComp<ActorComponent>(otherEntity) ? LogImpact.Extreme : LogImpact.High,
                $"Projectile {ToPrettyString(uid):projectile} shot by {ToPrettyString(component.Shooter):user} hit {otherName:target} and dealt {modifiedDamage.Total:damage} damage");
        }

        if (!deleted)
        {
            _guns.PlayImpactSound(otherEntity, modifiedDamage, component.SoundHit, component.ForceSound);
            _sharedCameraRecoil.KickCamera(otherEntity, direction);
        }

        var ev = new ProjectileCollideEvent(uid, false);
        RaiseLocalEvent(args.OtherEntity, ref ev);

        if (!ev.Cancelled)
        {
            component.DamagedEntity = true;

            if (component.DeleteOnCollide)
                QueueDel(uid);

            if (component.CanPenetrate)
            {
                if (!component.PenetrationModifierAdded)
                {
                    if (gun != null)
                        component.PenetrationStrength += gun.PenetrationModifier;

                    component.PenetrationModifierAdded = true;
                }
                if (component.PenetrationStrength <= 0)
                    QueueDel(uid);
                //TODO: Add a penetration resistance value to every entity that can be penetrated instead of a static 0.5 for mobs and 1 for everything else
                if (TryComp<MobStateComponent>(otherEntity, out var mobState))
                {
                    component.PenetrationStrength -= 0.5f;
                }
                else if (component.PenetrationStrength < 1 || component.CanPenetrateWall == false)
                {
                    QueueDel(uid);
                }
                else component.PenetrationStrength -= 1f;

                component.Damage *= component.PenetrationDamageFalloffMultiplier;
                component.DamagedEntity = false;
            }

            if (component.ImpactEffect != null && TryComp<TransformComponent>(uid, out var xform))
            {
                RaiseNetworkEvent(new ImpactEffectEvent(component.ImpactEffect, xform.Coordinates), Filter.Pvs(xform.Coordinates, entityMan: EntityManager));
            }
        }
    }
}
