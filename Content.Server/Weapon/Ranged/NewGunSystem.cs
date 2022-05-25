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
using Robust.Shared.Utility;

namespace Content.Server.Weapon.Ranged;

public sealed partial class NewGunSystem : SharedNewGunSystem
{
    [Dependency] private readonly EffectSystem _effects = default!;

    public override void Shoot(EntityUid gun, List<IShootable> ammo, EntityCoordinates fromCoordinates, EntityCoordinates toCoordinates, EntityUid? user = null)
    {
        // TODO recoil / spread
        var fromMap = fromCoordinates.ToMap(EntityManager);
        var toMap = toCoordinates.ToMapPos(EntityManager);
        var mapDirection = toMap - fromMap.Position;
        var entityDirection = Transform(fromCoordinates.EntityId).InvWorldMatrix.Transform(toMap) - fromCoordinates.Position;

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
                        MuzzleFlash(gun, cartridge, user);
                    }

                    EjectCartridge(cartridge.Owner);
                    Dirty(cartridge);
                    break;
                // Ammo shoots itself
                case NewAmmoComponent newAmmo:
                    ShootProjectile(newAmmo.Owner, mapDirection, user);
                    MuzzleFlash(gun, newAmmo, user);
                    break;
                case HitscanPrototype hitscan:
                    var ray = new CollisionRay(fromMap.Position, mapDirection.Normalized, hitscan.CollisionMask);
                    var rayCastResults = Physics.IntersectRay(fromMap.MapId, ray, hitscan.MaxLength, user, false).ToList();

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
        physics.LinearVelocity = direction.Normalized * 20f;

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
        // Lord
        // Forgive me for the shitcode I am about to do
        // Effects tempt me not
        var sprites = new List<(EntityCoordinates coordinates, Angle angle, SpriteSpecifier sprite, float scale)>();

        // We'll get the effects relative to the grid / map of the firer
        // TODO: Don't do muzzle or impact for short stuff

        if (hitscan.MuzzleFlash != null)
        {
            sprites.Add((fromCoordinates.Offset(angle.ToVec().Normalized / 2), angle, hitscan.MuzzleFlash, 1f));
        }

        if (hitscan.TravelFlash != null)
        {
            sprites.Add((fromCoordinates.Offset(angle.ToVec() * (distance + 0.5f) / 2), angle, hitscan.TravelFlash, distance - 1.5f));
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
