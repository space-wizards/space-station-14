using System.Linq;
using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Interaction;
using Content.Server.Mech.Equipment.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Weapons.Ranged.Components;
using Content.Server.Stunnable;
using Content.Server.Stunnable.Components;
using Content.Server.Emp;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.Interaction.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Projectiles;
using Content.Shared.StatusEffect;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Reflect;
using Content.Shared.Damage.Components;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Containers;
using Content.Shared._Starlight.Weapon.Components;
using Robust.Shared.Physics.Dynamics;
using Content.Shared.Movement.Components;
using Robust.Shared.Random;
using Content.Shared.Decals;
using Content.Server.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Timing;
using Content.Server.Decals;
using System;
using Content.Server.IgnitionSource;
using Content.Server.Atmos.EntitySystems;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Content.Server.Atmos.Components;
using Content.Shared._Starlight.Weapon;
using Robust.Shared.Maths;
using static Content.Server.Starlight.TextToSpeech.TTSManager;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using System.Collections.Generic;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem : SharedGunSystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IRobustRandom _rand = default!; // 🌟Starlight🌟
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly DamageExamineSystem _damageExamine = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly TransformSystem _transform = default!;  // 🌟Starlight🌟
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;  // 🌟Starlight🌟
    [Dependency] private readonly DecalSystem _decals = default!;  // 🌟Starlight🌟
    [Dependency] private readonly FlammableSystem _flammableSystem = default!; // 🌟Starlight🌟
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!; // 🌟Starlight🌟
    [Dependency] private readonly StunSystem _stunSystem = default!; // 🌟Starlight🌟
    [Dependency] private readonly EmpSystem _emp = default!; // 🌟Starlight🌟

    private const float DamagePitchVariation = 0.05f;
    private string[] _bloodDecals = []; // 🌟Starlight🌟

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BallisticAmmoProviderComponent, PriceCalculationEvent>(OnBallisticPrice);
        CacheDecals();
    }
    private void CacheDecals() // 🌟Starlight🌟
    {
        _bloodDecals = _proto.EnumeratePrototypes<DecalPrototype>().Where(x => x.Tags.Contains("BloodSplatter")).Select(x => x.ID).ToArray();
    }

    private void OnBallisticPrice(EntityUid uid, BallisticAmmoProviderComponent component, ref PriceCalculationEvent args)
    {
        if (string.IsNullOrEmpty(component.Proto) || component.UnspawnedCount == 0)
            return;

        if (!ProtoManager.TryIndex<EntityPrototype>(component.Proto, out var proto))
        {
            Log.Error($"Unable to find fill prototype for price on {component.Proto} on {ToPrettyString(uid)}");
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

        if (user != null)
        {
            var selfEvent = new SelfBeforeGunShotEvent(user.Value, (gunUid, gun), ammo);
            RaiseLocalEvent(user.Value, selfEvent);
            if (selfEvent.Cancelled)
            {
                userImpulse = false;
                return;
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

        var pointerLength = mapDirection.Length(); // 🌟Starlight🌟
        // Update shot based on the recoil
        toMap = fromMap.Position + (angle.ToVec() * pointerLength); // 🌟Starlight🌟
        mapDirection = toMap - fromMap.Position;
        var gunVelocity = Physics.GetMapLinearVelocity(fromEnt);

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
                //🌟Starlight🌟
                case HitScanCartridgeAmmoComponent cartridge:
                    if (!cartridge.Spent)
                    {
                        var hitscanPrototype = ProtoManager.Index(cartridge.Hitscan);

                        if (hitscanPrototype.Count > 1)
                        {
                            var spread = (hitscanPrototype.Spread + gun.Spread) / 2;
                            var spreadEvent = new GunGetAmmoSpreadEvent(spread);
                            RaiseLocalEvent(gunUid, ref spreadEvent);

                            var angles = LinearSpreadWithRandom(mapAngle - (spreadEvent.Spread / 2),
                                mapAngle + (spreadEvent.Spread / 2), hitscanPrototype.Count,
                                3f);

                            List<List<(EntityCoordinates, float, Angle, EntityUid?)>> hits = new(hitscanPrototype.Count);
                            for (var i = 0; i < hitscanPrototype.Count; i++)
                                hits.Add(Hitscan(gunUid, gun, fromCoordinates, user, fromMap, pointerLength, angles[i].ToVec(), hitscanPrototype));

                            FireEffects(hits.ToList(), hitscanPrototype);
                        }
                        else
                        {
                            var hits = Hitscan(gunUid, gun, fromCoordinates, user, fromMap, pointerLength, mapDirection, hitscanPrototype);
                            FireEffects([hits], hitscanPrototype);
                        }

                        RaiseLocalEvent(ent!.Value, new AmmoShotEvent()
                        {
                            FiredProjectiles = shotProjectiles,
                        });

                        SetCartridgeSpent(ent!.Value, cartridge, true);

                        if (cartridge.DeleteOnSpawn)
                            Del(ent.Value);
                    }
                    else
                    {
                        userImpulse = false;
                        Audio.PlayPredicted(gun.SoundEmpty, gunUid, user);
                    }

                    // Something like ballistic might want to leave it in the container still
                    if (!cartridge.DeleteOnSpawn && !Containers.IsEntityInContainer(ent!.Value) && !gun.Pump)
                        EjectCartridge(ent.Value, angle);

                    Dirty(ent!.Value, cartridge);
                    break;
                // Cartridge shoots something else
                case CartridgeAmmoComponent cartridge:
                    if (!cartridge.Spent)
                    {
                        var uid = Spawn(cartridge.Prototype, fromEnt);
                        CreateAndFireProjectiles(uid, cartridge);

                        RaiseLocalEvent(ent!.Value, new AmmoShotEvent()
                        {
                            FiredProjectiles = shotProjectiles,
                        });

                        SetCartridgeSpent(ent.Value, cartridge, true);

                        if (cartridge.DeleteOnSpawn)
                            Del(ent.Value);
                    }
                    else
                    {
                        userImpulse = false;
                        Audio.PlayPredicted(gun.SoundEmpty, gunUid, user);
                    }

                    // Something like ballistic might want to leave it in the container still
                    if (!cartridge.DeleteOnSpawn && !Containers.IsEntityInContainer(ent!.Value) && !gun.Pump)
                        EjectCartridge(ent.Value, angle);

                    Dirty(ent!.Value, cartridge);
                    break;
                // Ammo shoots itself
                case AmmoComponent newAmmo:
                    if (ent == null)
                        break;
                    CreateAndFireProjectiles(ent.Value, newAmmo);

                    break;
                case HitscanPrototype hitscan:

                    EntityUid? lastHit = null;
                    List<(EntityCoordinates fromCoordinates, float distance, Angle mapDirection, EntityUid? hitEntity)> effects = [];

                    var from = fromMap;
                    // can't use map coords above because funny FireEffects
                    var fromEffect = fromCoordinates;
                    var dir = mapDirection.Normalized();

                    //in the situation when user == null, means that the cannon fires on its own (via signals). And we need the gun to not fire by itself in this case
                    var lastUser = user ?? gunUid;

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

                            // Check if laser is shot from in a container
                            if (!_container.IsEntityOrParentInContainer(lastUser))
                            {
                                // Checks if the laser should pass over unless targeted by its user
                                foreach (var collide in rayCastResults)
                                {
                                    if (collide.HitEntity != gun.Target &&
                                        CompOrNull<RequireProjectileTargetComponent>(collide.HitEntity)?.Active == true)
                                    {
                                        continue;
                                    }

                                    result = collide;
                                    break;
                                }
                            }

                            var hit = result.HitEntity;
                            lastHit = hit;

                            effects.Add((fromEffect, result.Distance, dir.Normalized().ToAngle(), hit));

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

                        if (TryComp<StatusEffectsComponent>(hitEntity, out var status))
                        {
                            _stunSystem.TryStun(hitEntity, TimeSpan.FromSeconds(hitscan.StunAmount), true, status);

                            _stunSystem.TryKnockdown(hitEntity, TimeSpan.FromSeconds(hitscan.KnockdownAmount), true, status);

                            _stunSystem.TrySlowdown(hitEntity, TimeSpan.FromSeconds(hitscan.SlowdownAmount), true, hitscan.WalkSpeedMultiplier, hitscan.RunSpeedMultiplier, status);
                        }

                        if (hitscan.Ignite)
                        {
                            if (TryComp<FlammableComponent>(hitEntity, out var flammable))
                                _flammableSystem.SetFireStacks(hitEntity, flammable.FireStacks + (flammable.MinIgnitionTemperature / hitscan.Temperature), flammable, true);

                            if (Transform(hitEntity) is TransformComponent xform && xform.GridUid is { } hitGridUid)
                            {
                                var position = _transform.GetGridOrMapTilePosition(hitEntity, xform);
                                _atmosphere.HotspotExpose(hitGridUid, position, hitscan.Temperature, 50, user, true);
                            }
                        }

                        if (hitscan.Emp != null)
                            _emp.EmpPulse(_transform.GetMapCoordinates(hitEntity), hitscan.Emp.Range, hitscan.Emp.EnergyConsumption, hitscan.Emp.DisableDuration);

                        var dmg = hitscan.Damage;

                        var hitName = ToPrettyString(hitEntity);
                        if (dmg != null)
                            dmg = Damageable.TryChangeDamage(hitEntity, dmg, origin: user);

                        // check null again, as TryChangeDamage returns modified damage values
                        if (dmg != null)
                        {
                            if (!Deleted(hitEntity))
                            {
                                if (dmg.AnyPositive())
                                {
                                    _color.RaiseEffect(Color.Red, new List<EntityUid>() { hitEntity }, Filter.Pvs(hitEntity, entityManager: EntityManager));
                                }

                                // TODO get fallback position for playing hit sound.
                                PlayImpactSound(hitEntity, dmg, hitscan.Sound, hitscan.ForceSound);
                            }

                            if (user != null)
                            {
                                Logs.Add(LogType.HitScanHit,
                                    $"{ToPrettyString(user.Value):user} hit {hitName:target} using hitscan and dealt {dmg.GetTotal():damage} damage");
                            }
                            else
                            {
                                Logs.Add(LogType.HitScanHit,
                                    $"{hitName:target} hit by hitscan dealing {dmg.GetTotal():damage} damage");
                            }
                        }
                    }
                    else
                    {
                        effects.Add((fromEffect, hitscan.MaxLength, dir.ToAngle(), null));
                    }

                    FireEffects([effects], hitscan);

                    Audio.PlayPredicted(gun.SoundGunshotModified, gunUid, user);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        RaiseLocalEvent(gunUid, new AmmoShotEvent()
        {
            FiredProjectiles = shotProjectiles,
        });

        void CreateAndFireProjectiles(EntityUid ammoEnt, AmmoComponent ammoComp)
        {
            if (TryComp<ProjectileSpreadComponent>(ammoEnt, out var ammoSpreadComp))
            {
                var spreadEvent = new GunGetAmmoSpreadEvent(ammoSpreadComp.Spread);
                RaiseLocalEvent(gunUid, ref spreadEvent);

                var angles = LinearSpread(mapAngle - spreadEvent.Spread / 2,
                    mapAngle + spreadEvent.Spread / 2, ammoSpreadComp.Count);

                ShootOrThrow(ammoEnt, angles[0].ToVec(), gunVelocity, gun, gunUid, user);
                shotProjectiles.Add(ammoEnt);

                for (var i = 1; i < ammoSpreadComp.Count; i++)
                {
                    var newuid = Spawn(ammoSpreadComp.Proto, fromEnt);
                    ShootOrThrow(newuid, angles[i].ToVec(), gunVelocity, gun, gunUid, user);
                    shotProjectiles.Add(newuid);
                }
            }
            else
            {
                ShootOrThrow(ammoEnt, mapDirection, gunVelocity, gun, gunUid, user);
                shotProjectiles.Add(ammoEnt);
            }

            MuzzleFlash(gunUid, ammoComp, mapDirection.ToAngle(), user);
            Audio.PlayPredicted(gun.SoundGunshotModified, gunUid, user);
        }
        //🌟Starlight🌟
        List<(EntityCoordinates, float, Angle, EntityUid?)> Hitscan(EntityUid gunUid, GunComponent gun, EntityCoordinates fromCoordinates, EntityUid? user, MapCoordinates fromMap, float pointer, Vector2 mapDirection, HitscanPrototype hitscan)
        {
            EntityUid? lastHit = null;
            List<(EntityCoordinates fromCoordinates, float distance, Angle mapDirection, EntityUid? hitEntity)> effects = [];
            var accuracyZone = 3f;

            var from = fromMap;
            // can't use map coords above because funny FireEffects
            var fromEffect = fromCoordinates;
            var dir = mapDirection.Normalized();

            //in the situation when user == null, means that the cannon fires on its own (via signals). And we need the gun to not fire by itself in this case
            var lastUser = user ?? gunUid;

            if (hitscan.Reflective != ReflectType.None)
            {
                for (var reflectAttempt = 0; reflectAttempt < hitscan.Steps; reflectAttempt++)
                {
                    var ray = new CollisionRay(from.Position, dir, hitscan.CollisionMask);
                    var rayCastResults =
                        Physics.IntersectRay(from.MapId, ray, hitscan.MaxLength, lastUser, false).ToList();
                    if (!rayCastResults.Any())
                        break;

                    var result = rayCastResults[0];

                    // Check if laser is shot from in a container
                    if (!_container.IsEntityOrParentInContainer(lastUser))
                    {
                        // Checks if the laser should pass over unless targeted by its user
                        foreach (var collide in rayCastResults)
                        {
                            if (collide.HitEntity != gun.Target &&
                                CompOrNull<RequireProjectileTargetComponent>(collide.HitEntity)?.Active == true)
                            {
                                continue;
                            }
                            if (collide.Distance < pointer - 2f && HasComp<MobMoverComponent>(collide.HitEntity))
                            {
                                if (pointer - collide.Distance > 4f) continue;

                                var chance = Math.Clamp(1f - ((collide.Distance - 2f) / 2f), 0f, 1f);
                                if (!_rand.Prob(chance)) continue;
                            }

                            result = collide;
                            break;
                        }
                    }

                    var hit = result.HitEntity;
                    lastHit = hit;

                    effects.Add((fromEffect, result.Distance, dir.Normalized().ToAngle(), hit));

                    if (hitscan.Reflective != ReflectType.None)
                    {
                        var ev = new HitScanReflectAttemptEvent(user, gunUid, hitscan.Reflective, dir, false);
                        RaiseLocalEvent(hit, ref ev);

                        if (ev.Reflected)
                        {
                            fromEffect = Transform(hit).Coordinates;
                            from = TransformSystem.ToMapCoordinates(fromEffect);
                            dir = ev.Direction;
                            lastUser = hit;
                            continue;
                        }
                    }

                    if (hitscan.RicochetChance > 0f)
                    {
                        var ev = new HitScanRicochetAttemptEvent(hitscan.RicochetChance, from.Position, dir, false);
                        RaiseLocalEvent(hit, ref ev);

                        if (ev.Ricocheted)
                        {
                            fromEffect = _transform.ToCoordinates(result.HitEntity, new MapCoordinates(result.HitPos, fromMap.MapId));
                            from = TransformSystem.ToMapCoordinates(fromEffect);
                            dir = ev.Dir;
                            lastUser = hit;
                            continue;
                        }
                    }

                    Hit(user, hitscan, lastHit.Value);

                    if (hitscan.PierceChance >= 1f || (hitscan.PierceChance > 0f && _rand.Prob(hitscan.PierceChance)))
                    {
                        var ev = new HitScanPierceAttemptEvent(hitscan.PierceLevel, true);
                        RaiseLocalEvent(hit, ref ev);

                        if (ev.Pierced)
                        {
                            var random = Random.NextFloat(-hitscan.Derivation, hitscan.Derivation);
                            fromEffect = _transform.ToCoordinates(result.HitEntity, new MapCoordinates(result.HitPos, fromMap.MapId));
                            from = TransformSystem.ToMapCoordinates(fromEffect);
                            dir = (dir.ToAngle() + random).ToVec();
                            lastUser = hit;
                            continue;
                        }
                    }
                    break;
                }
                if (lastHit == null)
                    effects.Add((fromEffect, hitscan.MaxLength, dir.ToAngle(), null));

            }

            Audio.PlayPredicted(gun.SoundGunshotModified, gunUid, user);

            return effects;

            void Hit(EntityUid? user, HitscanPrototype hitscan, EntityUid hitEntity)
            {
                if (Deleted(hitEntity)) return;
                if (hitscan.StaminaDamage > 0f)
                    _stamina.TakeStaminaDamage(hitEntity, hitscan.StaminaDamage, source: user);

                var dmg = hitscan.Damage;

                var hitName = ToPrettyString(hitEntity);
                if (dmg != null)
                    dmg = Damageable.TryChangeDamage(hitEntity, dmg, ignoreResistances: hitscan.IgnoreResistances, origin: user, armorPenetration: hitscan.ArmorPenetration);

                // check null again, as TryChangeDamage returns modified damage values
                if (dmg != null)
                {
                    if (dmg.AnyPositive())
                    {
                        _color.RaiseEffect(Color.Red, [hitEntity], Filter.Pvs(hitEntity, entityManager: EntityManager));
                    }

                    // TODO get fallback position for playing hit sound.
                    PlayImpactSound(hitEntity, dmg, hitscan.Sound, hitscan.ForceSound);

                    if (user != null)
                    {
                        Logs.Add(LogType.HitScanHit,
                            $"{ToPrettyString(user.Value):user} hit {hitName:target} using hitscan and dealt {dmg.GetTotal():damage} damage");
                    }
                    else
                    {
                        Logs.Add(LogType.HitScanHit,
                            $"{hitName:target} hit by hitscan dealing {dmg.GetTotal():damage} damage");
                    }
                }
            }
        }
    }

    private void ShootOrThrow(EntityUid uid, Vector2 mapDirection, Vector2 gunVelocity, GunComponent gun, EntityUid gunUid, EntityUid? user)
    {
        if (gun.Target is { } target && !TerminatingOrDeleted(target))
        {
            var targeted = EnsureComp<TargetedProjectileComponent>(uid);
            targeted.Target = target;
            Dirty(uid, targeted);
        }

        // Do a throw
        if (!HasComp<ProjectileComponent>(uid))
        {
            RemoveShootable(uid);
            // TODO: Someone can probably yeet this a billion miles so need to pre-validate input somewhere up the call stack.
            ThrowingSystem.TryThrow(uid, mapDirection, gun.ProjectileSpeedModified, user);
            return;
        }

        ShootProjectile(uid, mapDirection, gunVelocity, gunUid, user, gun.ProjectileSpeedModified);
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

    // 🌟Starlight🌟
    private Angle[] LinearSpreadWithRandom(Angle start, Angle end, int intervals, float randomSpread)
    {
        var angles = new Angle[intervals];
        DebugTools.Assert(intervals > 1);

        for (var i = 0; i < intervals; i++)
        {
            var t = (float)i / (intervals - 1);
            var baseAngle = start + (end - start) * t;

            var randomFactor = _rand.NextFloat() - 0.5f;

            var randomOffset = Angle.FromDegrees(randomFactor * randomSpread);

            angles[i] = baseAngle + randomOffset;
        }

        return angles;
    }

    private Angle GetRecoilAngle(TimeSpan curTime, GunComponent component, Angle direction)
    {
        var timeSinceLastFire = (curTime - component.LastFire).TotalSeconds;
        var newTheta = MathHelper.Clamp(component.CurrentAngle.Theta + component.AngleIncreaseModified.Theta - component.AngleDecayModified.Theta * timeSinceLastFire, component.MinAngleModified.Theta, component.MaxAngleModified.Theta);
        component.CurrentAngle = new Angle(newTheta);
        component.LastFire = component.NextFire;

        // Convert it so angle can go either side.
        var random = Random.NextFloat(-0.5f, 0.5f);
        var spread = component.CurrentAngle.Theta * random;
        var angle = new Angle(direction.Theta + component.CurrentAngle.Theta * random);
        DebugTools.Assert(spread <= component.MaxAngleModified.Theta);
        return angle;
    }

    protected override void Popup(string message, EntityUid? uid, EntityUid? user) { }

    protected override void CreateEffect(EntityUid gunUid, MuzzleFlashEvent message, EntityUid? user = null)
    {
        var filter = Filter.Pvs(gunUid, entityManager: EntityManager);

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

        if (!forceWeaponSound && modifiedDamage != null && modifiedDamage.GetTotal() > 0 && TryComp<RangedDamageSoundComponent>(otherEntity, out var rangedSound))
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
    // 🌟Starlight🌟
    private void FireEffects(List<List<(EntityCoordinates fromCoordinates, float distance, Angle mapDirection, EntityUid? hitEntity)>> hits, HitscanPrototype hitscan)
    {
        if (hits.Count == 0) return;
        var hitscanEvent = new HitscanEvent
        {
            Hitscan = hitscan.ID,
            Effects = new Effect[hits.Count][]
        };
        var spreadIndex = -1;
        HashSet<EntityCoordinates> pvs = [];

        foreach (var hit in hits)
        {
            spreadIndex++;
            var index = -1;

            hitscanEvent.Effects[spreadIndex] = new Effect[hit.Count];
            ref var effects = ref hitscanEvent.Effects[spreadIndex];

            foreach (var (fromCoordinatesOrig, distance, mapDirection, hitEntity) in hit)
            {
                var fromCoordinates = fromCoordinatesOrig;
                var gridUid = _transform.GetGrid(fromCoordinates);
                var angle = mapDirection;

                var xformQuery = GetEntityQuery<TransformComponent>();
                if (xformQuery.TryGetComponent(gridUid, out var gridXform))
                {
                    var (_, gridRot, gridInvMatrix) = TransformSystem.GetWorldPositionRotationInvMatrix(gridXform, xformQuery);

                    fromCoordinates = new EntityCoordinates(gridUid.Value,
                        Vector2.Transform(_transform.ToMapCoordinates(fromCoordinates).Position, gridInvMatrix));

                    // Use the fallback angle I guess?
                    angle -= gridRot;
                }

                index++;
                effects[index] = new Effect
                {
                    Angle = angle,
                    Distance = distance,
                };
                ref var effect = ref effects[index];

                if (distance >= 1f)
                {
                    var muzzleCoords = fromCoordinates.Offset(angle.ToVec().Normalized() / 2);
                    var travelCoords = fromCoordinates.Offset(angle.ToVec() * (distance + 0.5f) / 2);
                    effect.MuzzleCoordinates = GetNetCoordinates(muzzleCoords);
                    effect.TravelCoordinates = GetNetCoordinates(travelCoords);
                }
                var impactCoords = fromCoordinates.Offset(angle.ToVec() * distance);
                effect.ImpactCoordinates = GetNetCoordinates(impactCoords);

                if (hitEntity is not null)
                {
                    if (hitscan.Reflective == ReflectType.NonEnergy)
                    {
                        if (TryComp<BloodstreamComponent>(hitEntity, out var bloodstream))
                        {
                            Timer.Spawn(200, () =>
                            {
                                var color = _proto.Index(bloodstream.BloodReagent).SubstanceColor;
                                // A flash of the neuralyzer, then a man in a black suit says that you didn’t see any “vector crutch” here, and if you did—read it again.
                                var coords = fromCoordinates.Offset((angle.ToVec() * (distance + 1.3f)) + new Vector2(-0.5f, -0.5f));
                                _decals.TryAddDecal(_rand.Pick(_bloodDecals), coords, out _, color, angle + Angle.FromDegrees(-45), cleanable: true);
                            });
                        }
                        else
                        {
                            effect.ImpactEnt = GetNetEntity(hitEntity.Value);
                        }
                    }
                }

                pvs.Add(fromCoordinates);
            }
        }


        if (pvs.Count > 0)
        {
            var filter = Filter.Empty();
            foreach (var pos in pvs.Where(x=>x.IsValid(EntityManager)))
                filter.Merge(Filter.Pvs(pos, entityMan: EntityManager));

            RaiseNetworkEvent(hitscanEvent, filter);
        }
    }

    #endregion
}
