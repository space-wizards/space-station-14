using System;
using Content.Server.Atmos.EntitySystems;
using Content.Server.CombatMode;
using Content.Server.Hands.Components;
using Content.Server.Interaction.Components;
using Content.Server.Stunnable;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;

namespace Content.Server.Weapon.Ranged;

public sealed partial class GunSystem
{
    /// <summary>
    /// Tries to fire a round of ammo out of the weapon.
    /// </summary>
    private void TryFire(EntityUid user, EntityCoordinates targetCoords, ServerRangedWeaponComponent gun)
    {
        if (!TryComp(user, out HandsComponent? hands) || hands.GetActiveHand?.Owner != gun.Owner)
            return;

        if (!TryComp(user, out CombatModeComponent? combat) || !combat.IsInCombatMode)
            return;

        if (!_blocker.CanInteract(user)) return;

        var fireAttempt = new GunFireAttemptEvent(user, gun);
        EntityManager.EventBus.RaiseLocalEvent(gun.Owner, fireAttempt);

        if (fireAttempt.Cancelled)
            return;

        var curTime = _gameTiming.CurTime;
        var span = curTime - gun.LastFireTime;
        if (span.TotalSeconds < 1 / gun.FireRate)
        {
            return;
        }

        // TODO: Clumsy should be eventbus I think?

        gun.LastFireTime = curTime;
        var coordinates = Transform(gun.Owner).Coordinates;

        if (gun.ClumsyCheck && gun.ClumsyDamage != null && ClumsyComponent.TryRollClumsy(user, gun.ClumsyExplodeChance))
        {
            //Wound them
            _damageable.TryChangeDamage(user, gun.ClumsyDamage);
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

        if (gun.CanHotspot)
            _atmos.HotspotExpose(coordinates, 700, 50);

        EntityManager.EventBus.RaiseLocalEvent(gun.Owner, new GunFireEvent());
    }

    /// <summary>
    /// Fires a round of ammo out of the weapon.
    /// </summary>
    /// <param name="shooter">Entity that is operating the weapon, usually the player.</param>
    /// <param name="targetPos">Target position on the map to shoot at.</param>
    private void Fire(EntityUid shooter, Vector2 targetPos)
    {
        if (ShotsLeft == 0)
        {
            SoundSystem.Play(Filter.Broadcast(), SoundEmpty.GetSound(), Owner);
            return;
        }

        var ammo = PeekAmmo();
        if (TakeProjectile(Entities.GetComponent<TransformComponent>(shooter).Coordinates) is not {Valid: true} projectile)
        {
            SoundSystem.Play(Filter.Broadcast(), SoundEmpty.GetSound(), Owner);
            return;
        }

        // At this point firing is confirmed
        var direction = (targetPos - Entities.GetComponent<TransformComponent>(shooter).WorldPosition).ToAngle();
        var angle = GetRecoilAngle(direction);
        // This should really be client-side but for now we'll just leave it here
        if (Entities.HasComponent<CameraRecoilComponent>(shooter))
        {
            var kick = -angle.ToVec() * 0.15f;
            EntitySystem.Get<CameraRecoilSystem>().KickCamera(shooter, kick);
        }

        // This section probably needs tweaking so there can be caseless hitscan etc.
        if (Entities.TryGetComponent(projectile, out HitscanComponent? hitscan))
        {
            FireHitscan(shooter, hitscan, angle);
        }
        else if (Entities.HasComponent<ProjectileComponent>(projectile) &&
                 Entities.TryGetComponent(ammo, out AmmoComponent? ammoComponent))
        {
            FireProjectiles(shooter, projectile, ammoComponent.ProjectilesFired, ammoComponent.EvenSpreadAngle, angle, ammoComponent.Velocity, ammo.Value);

            if (CanMuzzleFlash)
            {
                EntitySystem.Get<GunSystem>().MuzzleFlash(Owner, ammoComponent, angle);
            }

            if (ammoComponent.Caseless)
            {
                Entities.DeleteEntity(ammo.Value);
            }
        }
        else
        {
            // Invalid types
            throw new InvalidOperationException();
        }

        SoundSystem.Play(Filter.Broadcast(), SoundGunshot.GetSound(), Owner);

        LastFire = _gameTiming.CurTime;
    }

    #region Firing
    /// <summary>
    /// Handles firing one or many projectiles
    /// </summary>
    private void FireProjectiles(EntityUid shooter, EntityUid baseProjectile, int count, float evenSpreadAngle, Angle angle, float velocity, EntityUid ammo)
    {
        List<Angle>? sprayAngleChange = null;
        if (count > 1)
        {
            evenSpreadAngle *= SpreadRatio;
            sprayAngleChange = Linspace(-evenSpreadAngle / 2, evenSpreadAngle / 2, count);
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
                projectile = Entities.SpawnEntity(
                    Entities.GetComponent<MetaDataComponent>(baseProjectile).EntityPrototype?.ID,
                    Entities.GetComponent<TransformComponent>(baseProjectile).Coordinates);
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

            var physics = Entities.GetComponent<IPhysBody>(projectile);
            physics.BodyStatus = BodyStatus.InAir;

            var projectileComponent = Entities.GetComponent<ProjectileComponent>(projectile);
            projectileComponent.IgnoreEntity(shooter);

            // FIXME: Work around issue where inserting and removing an entity from a container,
            // then setting its linear velocity in the same tick resets velocity back to zero.
            // See SharedBroadphaseSystem.HandleContainerInsert()... It sets Awake to false, which causes this.
            projectile.SpawnTimer(TimeSpan.FromMilliseconds(25), () =>
            {
                Entities.GetComponent<IPhysBody>(projectile)
                    .LinearVelocity = projectileAngle.ToVec() * velocity;
            });


            Entities.GetComponent<TransformComponent>(projectile).WorldRotation = projectileAngle + MathHelper.PiOver2;
        }

        Entities.EventBus.RaiseLocalEvent(Owner, new GunShotEvent(firedProjectiles));
        Entities.EventBus.RaiseLocalEvent(ammo, new AmmoShotEvent(firedProjectiles));
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
    private void FireHitscan(EntityUid shooter, HitscanComponent hitscan, Angle angle)
    {
        var ray = new CollisionRay(Entities.GetComponent<TransformComponent>(Owner).Coordinates.ToMapPos(Entities), angle.ToVec(), (int) hitscan.CollisionMask);
        var physicsManager = EntitySystem.Get<SharedPhysicsSystem>();
        var rayCastResults = physicsManager.IntersectRay(Entities.GetComponent<TransformComponent>(Owner).MapID, ray, hitscan.MaxLength, shooter, false).ToList();

        if (rayCastResults.Count >= 1)
        {
            var result = rayCastResults[0];
            var distance = result.Distance;
            hitscan.FireEffects(shooter, distance, angle, result.HitEntity);
            var dmg = EntitySystem.Get<DamageableSystem>().TryChangeDamage(result.HitEntity, hitscan.Damage);
            if (dmg != null)
                EntitySystem.Get<AdminLogSystem>().Add(LogType.HitScanHit,
                    $"{Entities.ToPrettyString(shooter):user} hit {Entities.ToPrettyString(result.HitEntity):target} using {Entities.ToPrettyString(hitscan.Owner):used} and dealt {dmg.Total:damage} damage");
        }
        else
        {
            hitscan.FireEffects(shooter, hitscan.MaxLength, angle);
        }
    }
    #endregion
}
