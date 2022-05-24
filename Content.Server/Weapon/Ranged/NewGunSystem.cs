using System.Linq;
using Content.Server.Projectiles.Components;
using Content.Shared.Database;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Player;

namespace Content.Server.Weapon.Ranged;

public sealed partial class NewGunSystem : SharedNewGunSystem
{
    [Dependency] private readonly EffectSystem _effects = default!;

    public override void Shoot(List<IShootable> ammo, EntityCoordinates fromCoordinates, EntityCoordinates toCoordinates, EntityUid? user = null)
    {
        // TODO recoil / spread
        var fromMap = fromCoordinates.ToMap(EntityManager);
        var toMap = toCoordinates.ToMapPos(EntityManager);
        var mapDirection = toMap - fromMap.Position;

        // I must be high because this was getting tripped even when true.
        // DebugTools.Assert(direction != Vector2.Zero);

        foreach (var shootable in ammo)
        {
            switch (shootable)
            {
                // Cartridge shoots something itself
                case CartridgeAmmoComponent cartridge:
                    if (!cartridge.Spent)
                    {
                        var uid = Spawn(cartridge.Prototype, fromCoordinates);
                        ShootProjectile(uid, mapDirection, user);

                        if (TryComp<AppearanceComponent>(cartridge.Owner, out var appearance))
                            appearance.SetData(AmmoVisuals.Spent, true);

                        cartridge.Spent = true;
                    }

                    EjectCartridge(cartridge.Owner);
                    Dirty(cartridge);
                    break;
                // Ammo shoots itself
                case NewAmmoComponent newAmmo:
                    ShootProjectile(newAmmo.Owner, mapDirection, user);
                    break;
                case HitscanPrototype hitscan:
                    var ray = new CollisionRay(fromMap.Position, mapDirection.Normalized, hitscan.CollisionMask);
                    var rayCastResults = Physics.IntersectRay(fromMap.MapId, ray, hitscan.MaxLength, user, false).ToList();

                    var entityDirection = Transform(fromCoordinates.EntityId).InvWorldMatrix.Transform(toMap) - fromCoordinates.Position;

                    if (rayCastResults.Count >= 1)
                    {
                        var result = rayCastResults[0];
                        var distance = result.Distance;
                        FireEffects(fromCoordinates, distance, entityDirection.ToAngle(), hitscan, result.HitEntity);

                        var dmg = hitscan.Damage;

                        if (dmg != null)
                            dmg = Damageable.TryChangeDamage(result.HitEntity, dmg);

                        if (dmg != null)
                        {
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
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void ShootProjectile(EntityUid uid, Vector2 direction, EntityUid? user = null)
    {
        var physics = EnsureComp<PhysicsComponent>(uid);
        physics.BodyStatus = BodyStatus.InAir;
        physics.LinearVelocity = direction.Normalized * 30f;

        if (user != null)
        {
            var projectile = EnsureComp<ProjectileComponent>(uid);
            projectile.IgnoreEntity(user.Value);
        }

        Transform(uid).WorldRotation = direction.ToWorldAngle();
    }

    protected override void PlaySound(NewGunComponent gun, string? sound, EntityUid? user = null)
    {
        if (sound == null) return;

        SoundSystem.Play(Filter.Pvs(gun.Owner).RemoveWhereAttachedEntity(e => e == user), sound, gun.Owner);
    }

    protected override void Popup(string message, NewGunComponent gun, EntityUid? user) {}

    protected override void CreateEffect(EffectSystemMessage message, EntityUid? user = null)
    {
        // TODO: Fucking bad
        if (TryComp<ActorComponent>(user, out var actor))
        {
            _effects.CreateParticle(message, actor.PlayerSession);
        }
        else
        {
            _effects.CreateParticle(message);
        }
    }

    // TODO: Pseudo RNG so the client can predict these.
    #region Hitscan effects

    public void FireEffects(EntityCoordinates fromCoordinates, float distance, Angle angle, HitscanPrototype hitscan, EntityUid? hitEntity = null)
    {
        var startTime = Timing.CurTime;
        var endTime = startTime + TimeSpan.FromSeconds(MuzzleFlashLifetime);
        var color = new Vector4(hitscan.Color.R * 255, hitscan.Color.G * 255, hitscan.Color.B * 255, hitscan.Color.A * 255);

        // We'll get the effects relative to the grid / map of the firer
        var afterEffect = TravelFlash(fromCoordinates, angle, distance, hitscan, startTime, endTime, color);

        // Not predicted yet, SAD
        if (afterEffect != null)
        {
            _effects.CreateParticle(afterEffect);
            // CreateEffect(afterEffect);
        }

        // if we're too close we'll stop the impact and muzzle / impact sprites from clipping
        if (distance > 1.0f)
        {
            var impactEffect = ImpactFlash(fromCoordinates, angle.ToVec() * distance, hitscan, startTime, endTime, color);
            if (impactEffect != null)
            {
                _effects.CreateParticle(impactEffect);
                // CreateEffect(impactEffect);
            }

            var muzzleEffect = MuzzleFlash(fromCoordinates, angle, hitscan, startTime, endTime, color);
            if (muzzleEffect != null)
            {
                _effects.CreateParticle(muzzleEffect);
                // CreateEffect(muzzleEffect);
            }
        }
    }

    private EffectSystemMessage? MuzzleFlash(EntityCoordinates grid, Angle angle, HitscanPrototype hitscan, TimeSpan startTime, TimeSpan endTime, Vector4 color)
    {
        var sprite = hitscan.MuzzleFlash?.ToString();

        if (sprite == null)
            return null;

        var offset = angle.ToVec().Normalized / 2;

        var message = new EffectSystemMessage
        {
            EffectSprite = sprite,
            Born = startTime,
            DeathTime = endTime,
            Coordinates = grid.Offset(offset),
            //Rotated from east facing
            Rotation = (float) angle.Theta,
            Color = color,
            ColorDelta = new Vector4(0, 0, 0, -1500f),
            Shaded = false
        };

        return message;
    }

    private EffectSystemMessage? TravelFlash(
        EntityCoordinates origin,
        Angle angle,
        float distance,
        HitscanPrototype hitscan,
        TimeSpan startTime,
        TimeSpan endTime,
        Vector4 color)
    {
        var sprite = hitscan.TravelFlash?.ToString();

        if (sprite == null) return null;

        var midPointOffset = angle.ToVec() * (distance + 0.5f) / 2;
        var message = new EffectSystemMessage
        {
            EffectSprite = sprite,
            Born = startTime,
            DeathTime = endTime,
            Size = new Vector2(distance - 1.5f, 1f),
            Coordinates = origin.Offset(midPointOffset),
            //Rotated from east facing
            Rotation = (float) angle.Theta,
            Color = color,
            ColorDelta = new Vector4(0, 0, 0, -1500f),
            Shaded = false,
        };

        return message;
    }

    private EffectSystemMessage? ImpactFlash(EntityCoordinates coordinates, Vector2 offset, HitscanPrototype hitscan, TimeSpan startTime, TimeSpan endTime, Vector4 color)
    {
        var impact = hitscan.ImpactFlash?.ToString();

        if (impact == null)
            return null;

        var message = new EffectSystemMessage
        {
            EffectSprite = impact,
            Born = startTime,
            DeathTime = endTime,
            Coordinates = coordinates.Offset(offset),
            //Rotated from east facing
            Rotation = (float) offset.ToAngle().FlipPositive(),
            Color = color,
            ColorDelta = new Vector4(0, 0, 0, -1500f),
            Shaded = false,
        };

        return message;
    }

    #endregion
}
