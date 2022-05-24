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
        var direction = toCoordinates.ToMapPos(EntityManager) - fromMap.Position;

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
                        ShootProjectile(uid, direction, user);

                        if (TryComp<AppearanceComponent>(cartridge.Owner, out var appearance))
                        {
                            appearance.SetData(AmmoVisuals.Spent, true);
                        }

                        cartridge.Spent = true;
                    }

                    EjectCartridge(cartridge.Owner);
                    Dirty(cartridge);
                    break;
                // Ammo shoots itself
                case NewAmmoComponent newAmmo:
                    ShootProjectile(newAmmo.Owner, direction, user);
                    break;
                case HitscanPrototype hitscan:
                    var ray = new CollisionRay(fromMap.Position, direction.Normalized * MathF.Min(20f, direction.Length), hitscan.CollisionMask);
                    var rayCastResults = Physics.IntersectRay(fromMap.MapId, ray, hitscan.MaxLength, user, false).ToList();

                    if (rayCastResults.Count >= 1)
                    {
                        var result = rayCastResults[0];
                        var distance = result.Distance;
                        FireEffects(user, distance, direction.ToAngle(), result.HitEntity);
                        var dmg = Damageable.TryChangeDamage(result.HitEntity, hitscan.Damage);
                        if (dmg != null)
                            Logs.Add(LogType.HitScanHit,
                                $"{ToPrettyString(user):user} hit {ToPrettyString(result.HitEntity):target} using hitscan and dealt {dmg.Total:damage} damage");
                    }
                    else
                    {
                        FireEffects(user, hitscan.MaxLength, direction.ToAngle());
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

    public void FireEffects(MapCoordinates fromCoordinates, float distance, Angle angle, HitscanPrototype hitscan, EntityUid? hitEntity = null)
    {
        // We'll get the effects relative to the grid / map of the firer

        var afterEffect = AfterEffects(fromCoordinates, angle, distance, 1.0f);
        CreateEffect(afterEffect);

        // if we're too close we'll stop the impact and muzzle / impact sprites from clipping
        if (distance > 1.0f)
        {
            var impactEffect = ImpactFlash(distance, angle);
            if (impactEffect != null)
            {
                CreateEffect(impactEffect);
            }

            var muzzleEffect = MuzzleFlash(fromCoordinates, angle);
            if (muzzleEffect != null)
            {
                CreateEffect(muzzleEffect);
            }
        }

        // TODO: Sound
        if (hitEntity != null)
        {
            // TODO: No wall component so ?
            var offset = angle.ToVec().Normalized / 2;
            var coordinates = fromCoordinates.Offset(offset);
            // SoundSystem.Play(Filter.Pvs(coordinates), _soundHitWall.GetSound(), coordinates);
        }
    }

    private EffectSystemMessage? MuzzleFlash(EntityCoordinates grid, Angle angle, HitscanPrototype hitscan)
    {
        if (hitscan.MuzzleFlash == null)
        {
            return null;
        }

        var offset = angle.ToVec().Normalized / 2;

        var message = new EffectSystemMessage
        {
            EffectSprite = _muzzleFlash,
            Born = _startTime,
            DeathTime = _deathTime,
            Coordinates = grid.Offset(offset),
            //Rotated from east facing
            Rotation = (float) angle.Theta,
            Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), ColorModifier),
            ColorDelta = new Vector4(0, 0, 0, -1500f),
            Shaded = false
        };

        return message;
    }

    private EffectSystemMessage AfterEffects(EntityCoordinates origin, Angle angle, float distance, HitscanPrototype hitscan, float offset = 0.0f)
    {
        var midPointOffset = angle.ToVec() * distance / 2;
        var message = new EffectSystemMessage
        {
            EffectSprite = _spriteName,
            Born = _startTime,
            DeathTime = _deathTime,
            Size = new Vector2(distance - offset, 1f),
            Coordinates = origin.Offset(midPointOffset),
            //Rotated from east facing
            Rotation = (float) angle.Theta,
            Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), ColorModifier),
            ColorDelta = new Vector4(0, 0, 0, -1500f),

            Shaded = false
        };

        return message;
    }

    private EffectSystemMessage? ImpactFlash(float distance, Angle angle, HitscanPrototype hitscan)
    {
        var impact = hitscan.ImpactFlash?.ToString();

        if (impact == null)
            return null;

        var message = new EffectSystemMessage
        {
            EffectSprite = impact.ToString(),
            Born = _startTime,
            DeathTime = _deathTime,
            Coordinates = _entMan.GetComponent<TransformComponent>(Owner).Coordinates.Offset(angle.ToVec() * distance),
            //Rotated from east facing
            Rotation = (float) angle.FlipPositive(),
            Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), ColorModifier),
            ColorDelta = new Vector4(0, 0, 0, -1500f),
            Shaded = false
        };

        return message;
    }

    #endregion
}
