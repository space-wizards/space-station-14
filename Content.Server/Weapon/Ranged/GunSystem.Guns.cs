using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.CombatMode;
using Content.Server.Explosion.Components;
using Content.Server.Hands.Components;
using Content.Server.Interaction.Components;
using Content.Server.Projectiles.Components;
using Content.Server.Stunnable;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Weapon.Ranged;

public sealed partial class GunSystem
{
    /// <summary>
    /// Tries to fire a round of ammo out of the weapon.
    /// </summary>
    private void TryFire(EntityUid user, EntityCoordinates targetCoords, ServerRangedWeaponComponent gun)
    {
        if (!TryComp(gun.Owner, out ServerRangedBarrelComponent? barrel)) return;

        if (!TryComp(user, out HandsComponent? hands) || hands.GetActiveHand()?.HeldEntity != gun.Owner) return;

        if (!TryComp(user, out CombatModeComponent? combat) ||
            !combat.IsInCombatMode ||
            !_blocker.CanInteract(user, gun.Owner)) return;

        var fireAttempt = new GunFireAttemptEvent(user, gun);
        EntityManager.EventBus.RaiseLocalEvent(gun.Owner, fireAttempt);

        if (fireAttempt.Cancelled) return;

        var curTime = _gameTiming.CurTime;
        var span = curTime - gun.LastFireTime;
        if (span.TotalSeconds < 1 / barrel.FireRate) return;

        // TODO: Clumsy should be eventbus I think?

        gun.LastFireTime = curTime;
        var coordinates = Transform(gun.Owner).Coordinates;

        if (gun.ClumsyCheck && EntityManager.TryGetComponent<ClumsyComponent>(user, out var clumsyComponent) && ClumsyComponent.TryRollClumsy(user, gun.ClumsyExplodeChance))
        {
            //Wound them
            _damageable.TryChangeDamage(user, clumsyComponent.ClumsyDamage);
            _stun.TryParalyze(user, TimeSpan.FromSeconds(3f), true);

            // Apply salt to the wound ("Honk!")
            SoundSystem.Play(
                Filter.Pvs(gun.Owner), gun.ClumsyWeaponHandlingSound.GetSound(),
                coordinates, AudioParams.Default.WithMaxDistance(5));

            SoundSystem.Play(
                Filter.Pvs(gun.Owner), gun.ClumsyWeaponShotSound.GetSound(),
                coordinates, AudioParams.Default.WithMaxDistance(5));

            user.PopupMessage(Loc.GetString("server-ranged-weapon-component-try-fire-clumsy"));

            EntityManager.DeleteEntity(gun.Owner);
            return;
        }

        // Firing confirmed

        if (gun.CanHotspot)
            _atmos.HotspotExpose(coordinates, 700, 50);

        EntityManager.EventBus.RaiseLocalEvent(gun.Owner, new GunShotEvent());
        Fire(user, barrel, targetCoords);
    }

    /// <summary>
    /// Fires a round of ammo out of the weapon.
    /// </summary>
    private void Fire(EntityUid shooter, ServerRangedBarrelComponent component, EntityCoordinates coordinates)
    {
        if (component.ShotsLeft == 0)
        {
            SoundSystem.Play(Filter.Pvs(component.Owner), component.SoundEmpty.GetSound(), component.Owner);
            return;
        }

        var ammo = PeekAtAmmo(component);
        if (TakeOutProjectile(component, Transform(shooter).Coordinates) is not {Valid: true} projectile)
        {
            SoundSystem.Play(Filter.Pvs(component.Owner), component.SoundEmpty.GetSound(), component.Owner);
            return;
        }

        var targetPos = coordinates.ToMapPos(EntityManager);

        // At this point firing is confirmed
        var direction = (targetPos - Transform(shooter).WorldPosition).ToAngle();
        var angle = GetRecoilAngle(component, direction);
        // This should really be client-side but for now we'll just leave it here
        if (HasComp<CameraRecoilComponent>(shooter))
        {
            var kick = -angle.ToVec() * 0.15f;
            _recoil.KickCamera(shooter, kick);
        }

        // This section probably needs tweaking so there can be caseless hitscan etc.
        if (TryComp(projectile, out HitscanComponent? hitscan))
        {
            FireHitscan(shooter, projectile, angle, hitscan);
        }
        else if (HasComp<ProjectileComponent>(projectile) &&
                 TryComp(ammo, out AmmoComponent? ammoComponent))
        {
            var spread = component.SpreadRatio * ammoComponent.EvenSpreadAngle;
            var fired = FireProjectiles(shooter, projectile, ammoComponent.ProjectilesFired, spread, angle, ammoComponent.Velocity);

            EntityManager.EventBus.RaiseLocalEvent(component.Owner, new Barrels.Components.GunShotEvent(fired));
            EntityManager.EventBus.RaiseLocalEvent(ammo!.Value, new AmmoShotEvent(fired));

            if (component.CanMuzzleFlash)
            {
                MuzzleFlash(component.Owner, ammoComponent, angle);
            }

            if (ammoComponent.Caseless)
            {
                EntityManager.DeleteEntity(ammo.Value);
            }
        }
        else
        {
            // Invalid types
            throw new InvalidOperationException();
        }

        SoundSystem.Play(Filter.Broadcast(), component.SoundGunshot.GetSound(), component.Owner);

        component.Dirty(EntityManager);
        component.LastFire = _gameTiming.CurTime;
    }

    #region Firing
    /// <summary>
    /// Handles firing one or many projectiles
    /// </summary>
    public EntityUid[] FireProjectiles(EntityUid shooter, EntityUid baseProjectile, int count, float spread, Angle angle, float velocity)
    {
        List<Angle>? sprayAngleChange = null;
        if (count > 1)
        {
            sprayAngleChange = Linspace(-spread / 2, spread / 2, count);
        }

        var firedProjectiles = new EntityUid[count];
        for (var i = 0; i < count; i++)
        {
            EntityUid projectile;

            if (i == 0)
            {
                projectile = baseProjectile;
            }
            else
            {
                // TODO: Cursed as bruh
                projectile = EntityManager.SpawnEntity(
                    MetaData(baseProjectile).EntityPrototype?.ID,
                    Transform(baseProjectile).Coordinates);
            }

            firedProjectiles[i] = projectile;

            Angle projectileAngle;

            if (sprayAngleChange != null)
            {
                projectileAngle = angle + sprayAngleChange[i];
            }
            else
            {
                projectileAngle = angle;
            }

            var physics = EntityManager.GetComponent<IPhysBody>(projectile);
            physics.BodyStatus = BodyStatus.InAir;

            var projectileComponent = EntityManager.GetComponent<ProjectileComponent>(projectile);
            projectileComponent.IgnoreEntity(shooter);

            // FIXME: Work around issue where inserting and removing an entity from a container,
            // then setting its linear velocity in the same tick resets velocity back to zero.
            // See SharedBroadphaseSystem.HandleContainerInsert()... It sets Awake to false, which causes this.
            projectile.SpawnTimer(TimeSpan.FromMilliseconds(25), () =>
            {
                EntityManager.GetComponent<IPhysBody>(projectile)
                    .LinearVelocity = projectileAngle.ToVec() * velocity;
            });


            Transform(projectile).WorldRotation = projectileAngle + MathHelper.PiOver2;
        }

        return firedProjectiles;
    }

    /// <summary>
    ///     Returns a list of numbers that form a set of equal intervals between the start and end value. Used to calculate shotgun spread angles.
    /// </summary>
    private List<Angle> Linspace(double start, double end, int intervals)
    {
        DebugTools.Assert(intervals > 1);

        var linspace = new List<Angle>(intervals);

        for (var i = 0; i <= intervals - 1; i++)
        {
            linspace.Add(Angle.FromDegrees(start + (end - start) * i / (intervals - 1)));
        }
        return linspace;
    }

    /// <summary>
    /// Fires hitscan entities and then displays their effects
    /// </summary>
    public bool FireHitscan(EntityUid shooter, EntityUid projectile, Angle angle, HitscanComponent? hitscan = null)
    {
        if (!Resolve(projectile, ref hitscan, false))
            return false;

        var ray = new CollisionRay(Transform(shooter).WorldPosition, angle.ToVec(), (int) hitscan.CollisionMask);
        var rayCastResults = _physics.IntersectRay(Transform(shooter).MapID, ray, hitscan.MaxLength, shooter, false).ToList();

        if (rayCastResults.Count >= 1)
        {
            var result = rayCastResults[0];
            var distance = result.Distance;
            hitscan.FireEffects(shooter, distance, angle, result.HitEntity);
            var dmg = _damageable.TryChangeDamage(result.HitEntity, hitscan.Damage);
            if (dmg != null)
                _logs.Add(LogType.HitScanHit,
                    $"{EntityManager.ToPrettyString(shooter):user} hit {EntityManager.ToPrettyString(result.HitEntity):target} using {EntityManager.ToPrettyString(hitscan.Owner):used} and dealt {dmg.Total:damage} damage");
        }
        else
        {
            hitscan.FireEffects(shooter, hitscan.MaxLength, angle);
        }

        return true;
    }
    #endregion
}
