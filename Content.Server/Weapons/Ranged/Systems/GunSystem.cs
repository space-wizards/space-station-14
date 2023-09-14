using System.Linq;
using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Cargo.Systems;
using Content.Server.Interaction;
using Content.Server.Power.EntitySystems;
using Content.Server.Stunnable;
using Content.Server.Weapons.Ranged.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Reflect;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using SharedGunSystem = Content.Shared.Weapons.Ranged.Systems.SharedGunSystem;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem : SharedGunSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly DamageExamineSystem _damageExamine = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly StunSystem _stun = default!;

    public const float DamagePitchVariation = SharedMeleeWeaponSystem.DamagePitchVariation;
    public const float GunClumsyChance = 0.5f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BallisticAmmoProviderComponent, PriceCalculationEvent>(OnBallisticPrice);
    }

    private void OnBallisticPrice(EntityUid uid, BallisticAmmoProviderComponent component, ref PriceCalculationEvent args)
    {
        if (string.IsNullOrEmpty(component.FillProto) || component.UnspawnedCount == 0)
            return;

        if (!ProtoManager.TryIndex<EntityPrototype>(component.FillProto, out var proto))
        {
            Log.Error($"Unable to find fill prototype for price on {component.FillProto} on {ToPrettyString(uid)}");
            return;
        }

        // Probably good enough for most.
        var price = _pricing.GetEstimatedPrice(proto);
        args.Price += price * component.UnspawnedCount;
    }

    public override void Shoot(EntityUid gunUid, GunComponent gun, List<(EntityUid? Entity, IShootable Shootable)> ammo,
        EntityCoordinates fromCoordinates, EntityCoordinates toCoordinates, out bool userImpulse, EntityUid? user = null, bool throwItems = false)
    {
        userImpulse = true;

        // Try a clumsy roll
        // TODO: Who put this here
        if (TryComp<ClumsyComponent>(user, out var clumsy) && gun.ClumsyProof == false)
        {
            for (var i = 0; i < ammo.Count; i++)
            {
                if (_interaction.TryRollClumsy(user.Value, GunClumsyChance, clumsy))
                {
                    // Wound them
                    Damageable.TryChangeDamage(user, clumsy.ClumsyDamage, origin: user);
                    _stun.TryParalyze(user.Value, TimeSpan.FromSeconds(3f), true);

                    // Apply salt to the wound ("Honk!")
                    Audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/bang.ogg"), gunUid);
                    Audio.PlayPvs(clumsy.ClumsySound, gunUid);

                    PopupSystem.PopupEntity(Loc.GetString("gun-clumsy"), user.Value);
                    _adminLogger.Add(LogType.EntityDelete, LogImpact.Medium, $"Clumsy fire by {ToPrettyString(user.Value)} deleted {ToPrettyString(gunUid)}");
                    Del(gunUid);
                    userImpulse = false;
                    return;
                }
            }
        }

        var fromMap = fromCoordinates.ToMap(EntityManager, TransformSystem);
        var toMap = toCoordinates.ToMapPos(EntityManager, TransformSystem);
        var mapDirection = toMap - fromMap.Position;
        var mapAngle = mapDirection.ToAngle();
        var angle = GetRecoilAngle(Timing.CurTime, gun, mapDirection.ToAngle());

        // If applicable, this ensures the projectile is parented to grid on spawn, instead of the map.
        var fromEnt = MapManager.TryFindGridAt(fromMap, out var gridUid, out var grid)
            ? fromCoordinates.WithEntityId(gridUid, EntityManager)
            : new EntityCoordinates(MapManager.GetMapEntityId(fromMap.MapId), fromMap.Position);

        // Update shot based on the recoil
        toMap = fromMap.Position + angle.ToVec() * mapDirection.Length();
        mapDirection = toMap - fromMap.Position;
        var gunVelocity = Physics.GetMapLinearVelocity(gunUid);

        // I must be high because this was getting tripped even when true.
        // DebugTools.Assert(direction != Vector2.Zero);
        var shotProjectiles = new List<EntityUid>(ammo.Count);

        foreach (var (ent, shootable) in ammo)
        {
            // pneumatic cannon doesn't shoot bullets it just throws them, ignore ammo handling
            if (throwItems && ent != null)
            {
                ShootOrThrow(ent.Value, mapDirection, gunVelocity, gun, gunUid, user);
                continue;
            }

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
                                var uid = Spawn(cartridge.Prototype, fromEnt);
                                ShootOrThrow(uid, angles[i].ToVec(), gunVelocity, gun, gunUid, user);
                                shotProjectiles.Add(uid);
                            }
                        }
                        else
                        {
                            var uid = Spawn(cartridge.Prototype, fromEnt);
                            ShootOrThrow(uid, mapDirection, gunVelocity, gun, gunUid, user);
                            shotProjectiles.Add(uid);
                        }

                        RaiseLocalEvent(ent!.Value, new AmmoShotEvent()
                        {
                            FiredProjectiles = shotProjectiles,
                        });

                        SetCartridgeSpent(ent.Value, cartridge, true);
                        MuzzleFlash(gunUid, cartridge, user);
                        Audio.PlayPredicted(gun.SoundGunshot, gunUid, user);

                        if (cartridge.DeleteOnSpawn)
                            Del(ent.Value);
                    }
                    else
                    {
                        userImpulse = false;
                        Audio.PlayPredicted(gun.SoundEmpty, gunUid, user);
                    }

                    // Something like ballistic might want to leave it in the container still
                    if (!cartridge.DeleteOnSpawn && !Containers.IsEntityInContainer(ent!.Value))
                        EjectCartridge(ent.Value, angle);

                    Dirty(ent!.Value, cartridge);
                    break;
                // Ammo shoots itself
                case AmmoComponent newAmmo:
                    shotProjectiles.Add(ent!.Value);
                    MuzzleFlash(gunUid, newAmmo, user);
                    Audio.PlayPredicted(gun.SoundGunshot, gunUid, user);
                    ShootOrThrow(ent.Value, mapDirection, gunVelocity, gun, gunUid, user);
                    break;
                case HitscanPrototype hitscan:

                    EntityUid? lastHit = null;

                    var from = fromMap;
                    // can't use map coords above because funny FireEffects
                    var fromEffect = fromCoordinates;
                    var dir = mapDirection.Normalized();
                    var lastUser = user;

                    if (hitscan.Reflective != ReflectType.None)
                    {
                        for (var reflectAttempt = 0; reflectAttempt < 3; reflectAttempt++)
                        {
                            var ray = new CollisionRay(from.Position, dir, hitscan.CollisionMask);
                            var rayCastResults =
                                Physics.IntersectRay(from.MapId, ray, hitscan.MaxLength, lastUser, false).ToList();
                            if (!rayCastResults.Any())
                                break;

                            var result = rayCastResults[0];
                            var hit = result.HitEntity;
                            lastHit = hit;

                            FireEffects(fromEffect, result.Distance, dir.Normalized().ToAngle(), hitscan, hit);

                            var ev = new HitScanReflectAttemptEvent(user, gunUid, hitscan.Reflective, dir, false);
                            RaiseLocalEvent(hit, ref ev);

                            if (!ev.Reflected)
                                break;

                            fromEffect = Transform(hit).Coordinates;
                            from = fromEffect.ToMap(EntityManager, _transform);
                            dir = ev.Direction;
                            lastUser = hit;
                        }
                    }

                    if (lastHit != null)
                    {
                        var hitEntity = lastHit.Value;
                        if (hitscan.StaminaDamage > 0f)
                            _stamina.TakeStaminaDamage(hitEntity, hitscan.StaminaDamage, source: user);

                        var dmg = hitscan.Damage;

                        var hitName = ToPrettyString(hitEntity);
                        if (dmg != null)
                            dmg = Damageable.TryChangeDamage(hitEntity, dmg, origin: user);

                        // check null again, as TryChangeDamage returns modified damage values
                        if (dmg != null)
                        {
                            if (!Deleted(hitEntity))
                            {
                                if (dmg.Total > FixedPoint2.Zero)
                                {
                                    _color.RaiseEffect(Color.Red, new List<EntityUid>() { hitEntity }, Filter.Pvs(hitEntity, entityManager: EntityManager));
                                }

                                // TODO get fallback position for playing hit sound.
                                PlayImpactSound(hitEntity, dmg, hitscan.Sound, hitscan.ForceSound);
                            }

                            if (user != null)
                            {
                                Logs.Add(LogType.HitScanHit,
                                    $"{ToPrettyString(user.Value):user} hit {hitName:target} using hitscan and dealt {dmg.Total:damage} damage");
                            }
                            else
                            {
                                Logs.Add(LogType.HitScanHit,
                                    $"{hitName:target} hit by hitscan dealing {dmg.Total:damage} damage");
                            }
                        }
                    }
                    else
                    {
                        FireEffects(fromEffect, hitscan.MaxLength, dir.ToAngle(), hitscan);
                    }

                    Audio.PlayPredicted(gun.SoundGunshot, gunUid, user);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        RaiseLocalEvent(gunUid, new AmmoShotEvent()
        {
            FiredProjectiles = shotProjectiles,
        });
    }

    private void ShootOrThrow(EntityUid uid, Vector2 mapDirection, Vector2 gunVelocity, GunComponent gun, EntityUid gunUid, EntityUid? user)
    {
        // Do a throw
        if (!HasComp<ProjectileComponent>(uid))
        {
            RemoveShootable(uid);
            // TODO: Someone can probably yeet this a billion miles so need to pre-validate input somewhere up the call stack.
            ThrowingSystem.TryThrow(uid, mapDirection, gun.ProjectileSpeed, user);
            return;
        }

        ShootProjectile(uid, mapDirection, gunVelocity, gunUid, user, gun.ProjectileSpeed);
    }

    public void ShootProjectile(EntityUid uid, Vector2 direction, Vector2 gunVelocity, EntityUid gunUid, EntityUid? user = null, float speed = 20f)
    {
        var physics = EnsureComp<PhysicsComponent>(uid);
        Physics.SetBodyStatus(physics, BodyStatus.InAir);

        var targetMapVelocity = gunVelocity + direction.Normalized() * speed;
        var currentMapVelocity = Physics.GetMapLinearVelocity(uid, physics);
        var finalLinear = physics.LinearVelocity + targetMapVelocity - currentMapVelocity;
        Physics.SetLinearVelocity(uid, finalLinear, body: physics);

        if (user != null)
        {
            var projectile = EnsureComp<ProjectileComponent>(uid);
            Projectiles.SetShooter(projectile, user.Value);
            projectile.Weapon = gunUid;
        }

        TransformSystem.SetWorldRotation(uid, direction.ToWorldAngle());
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

    protected override void Popup(string message, EntityUid? uid, EntityUid? user) { }

    protected override void CreateEffect(EntityUid uid, MuzzleFlashEvent message, EntityUid? user = null)
    {
        var filter = Filter.Pvs(uid, entityManager: EntityManager);

        if (TryComp<ActorComponent>(user, out var actor))
            filter.RemovePlayer(actor.PlayerSession);

        RaiseNetworkEvent(message, filter);
    }

    public void PlayImpactSound(EntityUid otherEntity, DamageSpecifier? modifiedDamage, SoundSpecifier? weaponSound, bool forceWeaponSound)
    {
        DebugTools.Assert(!Deleted(otherEntity), "Impact sound entity was deleted");

        // Like projectiles and melee,
        // 1. Entity specific sound
        // 2. Ammo's sound
        // 3. Nothing
        var playedSound = false;

        if (!forceWeaponSound && modifiedDamage != null && modifiedDamage.Total > 0 && TryComp<RangedDamageSoundComponent>(otherEntity, out var rangedSound))
        {
            var type = SharedMeleeWeaponSystem.GetHighestDamageSound(modifiedDamage, ProtoManager);

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

    private void FireEffects(EntityCoordinates fromCoordinates, float distance, Angle mapDirection, HitscanPrototype hitscan, EntityUid? hitEntity = null)
    {
        // Lord
        // Forgive me for the shitcode I am about to do
        // Effects tempt me not
        var sprites = new List<(NetCoordinates coordinates, Angle angle, SpriteSpecifier sprite, float scale)>();
        var gridUid = fromCoordinates.GetGridUid(EntityManager);
        var angle = mapDirection;

        // We'll get the effects relative to the grid / map of the firer
        // Look you could probably optimise this a bit with redundant transforms at this point.
        var xformQuery = GetEntityQuery<TransformComponent>();

        if (xformQuery.TryGetComponent(gridUid, out var gridXform))
        {
            var (_, gridRot, gridInvMatrix) = TransformSystem.GetWorldPositionRotationInvMatrix(gridXform, xformQuery);

            fromCoordinates = new EntityCoordinates(gridUid.Value,
                gridInvMatrix.Transform(fromCoordinates.ToMapPos(EntityManager, TransformSystem)));

            // Use the fallback angle I guess?
            angle -= gridRot;
        }

        if (distance >= 1f)
        {
            if (hitscan.MuzzleFlash != null)
            {
                var coords = fromCoordinates.Offset(angle.ToVec().Normalized() / 2);
                var netCoords = GetNetCoordinates(coords);

                sprites.Add((netCoords, angle, hitscan.MuzzleFlash, 1f));
            }

            if (hitscan.TravelFlash != null)
            {
                var coords = fromCoordinates.Offset(angle.ToVec() * (distance + 0.5f) / 2);
                var netCoords = GetNetCoordinates(coords);

                sprites.Add((netCoords, angle, hitscan.TravelFlash, distance - 1.5f));
            }
        }

        if (hitscan.ImpactFlash != null)
        {
            var coords = fromCoordinates.Offset(angle.ToVec() * distance);
            var netCoords = GetNetCoordinates(coords);

            sprites.Add((netCoords, angle.FlipPositive(), hitscan.ImpactFlash, 1f));
        }

        if (sprites.Count > 0)
        {
            RaiseNetworkEvent(new HitscanEvent
            {
                Sprites = sprites,
            }, Filter.Pvs(fromCoordinates, entityMan: EntityManager));
        }
    }

    #endregion
}
