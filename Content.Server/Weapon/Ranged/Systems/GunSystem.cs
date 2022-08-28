using System.Linq;
using Content.Server.Damage.Systems;
using Content.Server.Projectiles.Components;
using Content.Server.Weapon.Melee;
using Content.Server.Weapon.Ranged.Components;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using SharedGunSystem = Content.Shared.Weapons.Ranged.Systems.SharedGunSystem;

namespace Content.Server.Weapon.Ranged.Systems;

public sealed partial class GunSystem : SharedGunSystem
{
    [Dependency] private readonly StaminaSystem _stamina = default!;

    public const float DamagePitchVariation = MeleeWeaponSystem.DamagePitchVariation;

    public override void Shoot(GunComponent gun, List<IShootable> ammo, EntityCoordinates fromCoordinates, EntityCoordinates toCoordinates, EntityUid? user = null)
    {
        var fromMap = fromCoordinates.ToMap(EntityManager);
        var toMap = toCoordinates.ToMapPos(EntityManager);
        var mapDirection = toMap - fromMap.Position;
        var mapAngle = mapDirection.ToAngle();
        var angle = GetRecoilAngle(Timing.CurTime, gun, mapDirection.ToAngle());

        // Update shot based on the recoil
        toMap = fromMap.Position + angle.ToVec() * mapDirection.Length;
        mapDirection = toMap - fromMap.Position;
        var entityDirection = Transform(fromCoordinates.EntityId).InvWorldMatrix.Transform(toMap) - fromCoordinates.Position;

        // I must be high because this was getting tripped even when true.
        // DebugTools.Assert(direction != Vector2.Zero);
        var shotProjectiles = new List<EntityUid>(ammo.Count);

        foreach (var shootable in ammo)
        {
            switch (shootable)
            {
                // Cartridge shoots something else
                case CartridgeAmmoComponent cartridge:
                    if (!cartridge.Spent)
                    {
                        if (cartridge.Count > 1)
                        {
                            var angles = LinearSpread(mapAngle - cartridge.Spread / 2,
                                mapAngle + cartridge.Spread / 2, cartridge.Count);

                            for (var i = 0; i < cartridge.Count; i++)
                            {
                                var uid = Spawn(cartridge.Prototype, fromCoordinates);
                                ShootProjectile(uid, angles[i].ToVec(), user, gun.ProjectileSpeed);
                                shotProjectiles.Add(uid);
                            }
                        }
                        else
                        {
                            var uid = Spawn(cartridge.Prototype, fromCoordinates);
                            ShootProjectile(uid, mapDirection, user, gun.ProjectileSpeed);
                            shotProjectiles.Add(uid);
                        }

                        SetCartridgeSpent(cartridge, true);
                        MuzzleFlash(gun.Owner, cartridge, user);
                        PlaySound(gun.Owner, gun.SoundGunshot?.GetSound(Random, ProtoManager), user);

                        if (cartridge.DeleteOnSpawn)
                            Del(cartridge.Owner);
                    }
                    else
                    {
                        PlaySound(gun.Owner, gun.SoundEmpty?.GetSound(Random, ProtoManager), user);
                    }

                    // Something like ballistic might want to leave it in the container still
                    if (!cartridge.DeleteOnSpawn && !Containers.IsEntityInContainer(cartridge.Owner))
                        EjectCartridge(cartridge.Owner);

                    Dirty(cartridge);
                    break;
                // Ammo shoots itself
                case AmmoComponent newAmmo:
                    shotProjectiles.Add(newAmmo.Owner);
                    MuzzleFlash(gun.Owner, newAmmo, user);
                    PlaySound(gun.Owner, gun.SoundGunshot?.GetSound(Random, ProtoManager), user);

                    // Do a throw
                    if (!HasComp<ProjectileComponent>(newAmmo.Owner))
                    {
                        RemComp<AmmoComponent>(newAmmo.Owner);
                        // TODO: Someone can probably yeet this a billion miles so need to pre-validate input somewhere up the call stack.
                        ThrowingSystem.TryThrow(newAmmo.Owner, mapDirection, gun.ProjectileSpeed, user);
                        break;
                    }

                    ShootProjectile(newAmmo.Owner, mapDirection, user, gun.ProjectileSpeed);
                    break;
                case HitscanPrototype hitscan:
                    var ray = new CollisionRay(fromMap.Position, mapDirection.Normalized, hitscan.CollisionMask);
                    var rayCastResults = Physics.IntersectRay(fromMap.MapId, ray, hitscan.MaxLength, user, false).ToList();

                    if (rayCastResults.Count >= 1)
                    {
                        var result = rayCastResults[0];
                        var distance = result.Distance;
                        FireEffects(fromCoordinates, distance, entityDirection.ToAngle(), hitscan, result.HitEntity);

                        if (hitscan.StaminaDamage > 0f)
                            _stamina.TakeStaminaDamage(result.HitEntity, hitscan.StaminaDamage);

                        var dmg = hitscan.Damage;

                        if (dmg != null)
                            dmg = Damageable.TryChangeDamage(result.HitEntity, dmg);

                        if (dmg != null)
                        {
                            PlayImpactSound(result.HitEntity, dmg, hitscan.Sound, hitscan.ForceSound);

                            if (user != null)
                            {
                                Logs.Add(LogType.HitScanHit,
                                    $"{ToPrettyString(user.Value):user} hit {ToPrettyString(result.HitEntity):target} using hitscan and dealt {dmg.Total:damage} damage");
                            }
                            else
                            {
                                Logs.Add(LogType.HitScanHit,
                                    $"Hit {ToPrettyString(result.HitEntity):target} using hitscan and dealt {dmg.Total:damage} damage");
                            }
                        }
                    }
                    else
                    {
                        FireEffects(fromCoordinates, hitscan.MaxLength, entityDirection.ToAngle(), hitscan);
                    }
                    PlaySound(gun.Owner, gun.SoundGunshot?.GetSound(Random, ProtoManager), user);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        RaiseLocalEvent(gun.Owner, new AmmoShotEvent()
        {
            FiredProjectiles = shotProjectiles,
        }, false);
    }

    public void ShootProjectile(EntityUid uid, Vector2 direction, EntityUid? user = null, float speed = 20f)
    {
        var physics = EnsureComp<PhysicsComponent>(uid);
        physics.BodyStatus = BodyStatus.InAir;
        physics.LinearVelocity = direction.Normalized * speed;

        if (user != null)
        {
            var projectile = EnsureComp<ProjectileComponent>(uid);
            Projectiles.SetShooter(projectile, user.Value);
        }

        Transform(uid).WorldRotation = direction.ToWorldAngle();
    }

    /// <summary>
    /// Gets a linear spread of angles between start and end.
    /// </summary>
    /// <param name="start">Start angle in degrees</param>
    /// <param name="end">End angle in degrees</param>
    /// <param name="intervals">How many shots there are</param>
    private Angle[] LinearSpread(Angle start, Angle end, int intervals)
    {
        var angles = new Angle[intervals];
        DebugTools.Assert(intervals > 1);

        for (var i = 0; i <= intervals - 1; i++)
        {
            angles[i] = new Angle(start + (end - start) * i / (intervals - 1));
        }

        return angles;
    }

    private Angle GetRecoilAngle(TimeSpan curTime, GunComponent component, Angle direction)
    {
        var timeSinceLastFire = (curTime - component.LastFire).TotalSeconds;
        var newTheta = MathHelper.Clamp(component.CurrentAngle.Theta + component.AngleIncrease.Theta - component.AngleDecay.Theta * timeSinceLastFire, component.MinAngle.Theta, component.MaxAngle.Theta);
        component.CurrentAngle = new Angle(newTheta);
        component.LastFire = component.NextFire;

        // Convert it so angle can go either side.
        var random = Random.NextFloat(-0.5f, 0.5f);
        var spread = component.CurrentAngle.Theta * random;
        var angle = new Angle(direction.Theta + component.CurrentAngle.Theta * random);
        DebugTools.Assert(spread <= component.MaxAngle.Theta);
        return angle;
    }

    protected override void PlaySound(EntityUid gun, string? sound, EntityUid? user = null)
    {
        if (string.IsNullOrEmpty(sound)) return;

        SoundSystem.Play(sound, Filter.Pvs(gun, entityManager: EntityManager).RemoveWhereAttachedEntity(e => e == user), gun);
    }

    protected override void Popup(string message, EntityUid? uid, EntityUid? user) {}

    protected override void CreateEffect(EntityUid uid, MuzzleFlashEvent message, EntityUid? user = null)
    {
        var filter = Filter.Pvs(uid, entityManager: EntityManager);

        if (TryComp<ActorComponent>(user, out var actor))
            filter.RemovePlayer(actor.PlayerSession);

        RaiseNetworkEvent(message, filter);
    }

    public void PlayImpactSound(EntityUid otherEntity, DamageSpecifier? modifiedDamage, SoundSpecifier? weaponSound, bool forceWeaponSound)
    {
        if (Deleted(otherEntity))
            return;

        // Like projectiles and melee,
        // 1. Entity specific sound
        // 2. Ammo's sound
        // 3. Nothing
        var playedSound = false;

        if (!forceWeaponSound && modifiedDamage != null && modifiedDamage.Total > 0 && TryComp<RangedDamageSoundComponent>(otherEntity, out var rangedSound))
        {
            var type = MeleeWeaponSystem.GetHighestDamageSound(modifiedDamage, ProtoManager);

            if (type != null && rangedSound.SoundTypes?.TryGetValue(type, out var damageSoundType) == true)
            {
                Audio.PlayPvs(damageSoundType, otherEntity, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (type != null && rangedSound.SoundGroups?.TryGetValue(type, out var damageSoundGroup) == true)
            {
                Audio.PlayPvs(damageSoundGroup, otherEntity, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
        }

        if (!playedSound && weaponSound != null)
        {
            Audio.PlayPvs(weaponSound, otherEntity);
        }
    }

    // TODO: Pseudo RNG so the client can predict these.
    #region Hitscan effects

    private void FireEffects(EntityCoordinates fromCoordinates, float distance, Angle angle, HitscanPrototype hitscan, EntityUid? hitEntity = null)
    {
        // Lord
        // Forgive me for the shitcode I am about to do
        // Effects tempt me not
        var sprites = new List<(EntityCoordinates coordinates, Angle angle, SpriteSpecifier sprite, float scale)>();

        // We'll get the effects relative to the grid / map of the firer
        if (distance >= 1f)
        {
            if (hitscan.MuzzleFlash != null)
            {
                sprites.Add((fromCoordinates.Offset(angle.ToVec().Normalized / 2), angle, hitscan.MuzzleFlash, 1f));
            }

            if (hitscan.TravelFlash != null)
            {
                sprites.Add((fromCoordinates.Offset(angle.ToVec() * (distance + 0.5f) / 2), angle, hitscan.TravelFlash, distance - 1.5f));
            }
        }

        if (hitscan.ImpactFlash != null)
        {
            sprites.Add((fromCoordinates.Offset(angle.ToVec() * distance), angle.FlipPositive(), hitscan.ImpactFlash, 1f));
        }

        if (sprites.Count > 0)
        {
            RaiseNetworkEvent(new HitscanEvent()
            {
                Sprites = sprites,
            }, Filter.Pvs(fromCoordinates, entityMan: EntityManager));
        }
    }

    #endregion
}
