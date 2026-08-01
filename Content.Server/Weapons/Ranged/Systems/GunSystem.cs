using System.Numerics;
using System.Linq;
using Content.Server.Construction;
using Content.Server.Cargo.Systems;
using Content.Server.DeadSpace.Weapons.Ranged;
using Content.Shared.Cargo;
using Content.Shared.Damage;
using Content.Shared.DeadSpace.CCCCVars;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem : SharedGunSystem
{
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

    // DS14-start
    private readonly Dictionary<EntityUid, BallisticConstructionTransferData> _ballisticConstructionTransfers = new();
    // DS14-end

    private const float DamagePitchVariation = 0.05f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BallisticAmmoProviderComponent, PriceCalculationEvent>(OnBallisticPrice);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, ConstructionChangeEntityEvent>(OnBallisticConstructionChange); // DS14
        SubscribeLocalEvent<BallisticAmmoProviderComponent, AfterConstructionChangeEntityEvent>(OnBallisticAfterConstructionChange); // DS14
    }

    // DS14-start
    private void OnBallisticConstructionChange(Entity<BallisticAmmoProviderComponent> ent, ref ConstructionChangeEntityEvent args)
    {
        if (ent.Owner != args.Old)
            return;

        _ballisticConstructionTransfers[args.New] = new BallisticConstructionTransferData(
            ent.Comp.Proto,
            ent.Comp.UnspawnedCount,
            ent.Comp.Entities.ToArray());
    }

    private void OnBallisticAfterConstructionChange(Entity<BallisticAmmoProviderComponent> ent, ref AfterConstructionChangeEntityEvent args)
    {
        if (_ballisticConstructionTransfers.Remove(ent.Owner, out var transfer))
        {
            ent.Comp.Proto = transfer.Proto;
            ent.Comp.UnspawnedCount = transfer.UnspawnedCount;
            ent.Comp.Entities.Clear();
            ent.Comp.Entities.AddRange(transfer.Entities.Where(Exists));
        }

        foreach (var contained in ent.Comp.Container.ContainedEntities)
        {
            if (!ent.Comp.Entities.Contains(contained))
                ent.Comp.Entities.Add(contained);
        }

        UpdateBallisticAppearance(ent);
        UpdateAmmoCount(ent);
        DirtyField(ent.AsNullable(), nameof(BallisticAmmoProviderComponent.Entities));
        DirtyField(ent.AsNullable(), nameof(BallisticAmmoProviderComponent.UnspawnedCount));
    }

    private readonly record struct BallisticConstructionTransferData(
        EntProtoId? Proto,
        int UnspawnedCount,
        EntityUid[] Entities);
    // DS14-end

    private void OnBallisticPrice(Entity<BallisticAmmoProviderComponent> ent, ref PriceCalculationEvent args)
    {
        if (string.IsNullOrEmpty(ent.Comp.Proto) || ent.Comp.UnspawnedCount == 0)
            return;

        if (!ProtoManager.TryIndex<EntityPrototype>(ent.Comp.Proto, out var proto))
        {
            Log.Error($"Unable to find fill prototype for price on {ent.Comp.Proto} on {ToPrettyString(ent)}");
            return;
        }

        // Probably good enough for most.
        var price = _pricing.GetEstimatedPrice(proto);
        args.Price += price * ent.Comp.UnspawnedCount;
    }

    public override void Shoot(Entity<GunComponent> gun, List<(EntityUid? Entity, IShootable Shootable)> ammo,
        EntityCoordinates fromCoordinates, EntityCoordinates toCoordinates, out bool userImpulse, EntityUid? user = null, bool throwItems = false)
    {
        userImpulse = true;

        if (user != null)
        {
            var selfEvent = new SelfBeforeGunShotEvent(user.Value, gun, ammo);
            RaiseLocalEvent(user.Value, selfEvent);
            if (selfEvent.Cancelled)
            {
                userImpulse = false;
                return;
            }
        }

        if (!TryGetShootMapDirection(fromCoordinates, toCoordinates, out var fromMap, out var mapDirection))
        {
            userImpulse = false;
            return;
        }

        var mapAngle = mapDirection.ToAngle();
        var angle = GetRecoilAngle(Timing.CurTime, gun, mapAngle);

        // If applicable, this ensures the projectile is parented to grid on spawn, instead of the map.
        var fromEnt = MapManager.TryFindGridAt(fromMap, out var gridUid, out _)
            ? TransformSystem.WithEntityId(fromCoordinates, gridUid)
            : new EntityCoordinates(_map.GetMapOrInvalid(fromMap.MapId), fromMap.Position);

        // Update shot based on the recoil
        var toMap = fromMap.Position + angle.ToVec() * mapDirection.Length();
        mapDirection = toMap - fromMap.Position;
        var gunVelocity = Physics.GetMapLinearVelocity(fromEnt);

        // I must be high because this was getting tripped even when true.
        // DebugTools.Assert(direction != Vector2.Zero);
        var shotProjectiles = new List<EntityUid>(ammo.Count);
        ushort nextProjectileIndex = 0;

        foreach (var (ent, shootable) in ammo)
        {
            // pneumatic cannon doesn't shoot bullets it just throws them, ignore ammo handling
            if (throwItems && ent != null)
            {
                FireProjectile(ent.Value, mapDirection);
                continue;
            }

            // TODO: Clean this up in a gun refactor at some point - too much copy pasting
            switch (shootable)
            {
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
                        Audio.PlayPredicted(gun.Comp.SoundEmpty, gun, user);
                    }

                    // Something like ballistic might want to leave it in the container still
                    if (!cartridge.DeleteOnSpawn && !Containers.IsEntityInContainer(ent!.Value))
                        EjectCartridge(ent.Value, angle);

                    Dirty(ent!.Value, cartridge);
                    break;
                // Ammo shoots itself
                case AmmoComponent newAmmo:
                    if (ent == null)
                        break;
                    CreateAndFireProjectiles(ent.Value, newAmmo);

                    break;
                case HitscanAmmoComponent:
                    if (ent == null)
                        break;

                    CreateAndFireProjectiles(ent.Value, null);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        RaiseLocalEvent(gun, new AmmoShotEvent()
        {
            FiredProjectiles = shotProjectiles,
        });

        void CreateAndFireProjectiles(EntityUid ammoEnt, AmmoComponent? ammoComp)
        {
            var firedHitscan = HasComp<HitscanAmmoComponent>(ammoEnt);

            if (TryComp<ProjectileSpreadComponent>(ammoEnt, out var ammoSpreadComp))
            {
                var spreadEvent = new GunGetAmmoSpreadEvent(ammoSpreadComp.Spread);
                RaiseLocalEvent(gun, ref spreadEvent);

                var angles = LinearSpread(angle - spreadEvent.Spread / 2,
                    angle + spreadEvent.Spread / 2, ammoSpreadComp.Count);

                if (FireProjectile(ammoEnt, angles[0].ToVec()))
                    shotProjectiles.Add(ammoEnt);

                for (var i = 1; i < ammoSpreadComp.Count; i++)
                {
                    var newuid = Spawn(ammoSpreadComp.Proto, fromEnt);
                    if (FireProjectile(newuid, angles[i].ToVec()))
                        shotProjectiles.Add(newuid);
                }
            }
            else
            {
                if (FireProjectile(ammoEnt, mapDirection))
                    shotProjectiles.Add(ammoEnt);
            }

            if (ammoComp != null && !firedHitscan)
                MuzzleFlash(gun, ammoComp, mapDirection.ToAngle(), user);

            Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, user);
        }

        bool FireProjectile(EntityUid projectile, Vector2 direction)
        {
            if (!ShootOrThrow(projectile, direction, gunVelocity, gun, user, nextProjectileIndex))
                return false;

            nextProjectileIndex++;
            return true;
        }
    }

    private bool ShootOrThrow(
        EntityUid uid,
        Vector2 mapDirection,
        Vector2 gunVelocity,
        Entity<GunComponent> gun,
        EntityUid? user,
        ushort projectileIndex)
    {
        ApplyExecutionShotDamage(uid, gun); // DS14

        // DS14-start: cartridge-spawned hitscans bypass projectile physics entirely.
        if (HasComp<HitscanAmmoComponent>(uid))
        {
            var hitscanEv = new HitscanTraceEvent
            {
                FromCoordinates = Transform(uid).Coordinates,
                ShotDirection = mapDirection.Normalized(),
                Gun = gun,
                Shooter = user,
                Target = gun.Comp.Target,
                PredictionId = gun.Comp.PredictionId,
            };

            RaiseLocalEvent(uid, ref hitscanEv);
            Del(uid);
            return false;
        }
        // DS14-end

        if (gun.Comp.Target is { } target && !TerminatingOrDeleted(target))
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
            ThrowingSystem.TryThrow(uid, mapDirection, gun.Comp.ProjectileSpeedModified, user);
            return false;
        }

        if (gun.Comp.PredictionId != 0 &&
            user != null &&
            _config.GetCVar(CCCCVars.ProjectilePredictionEnabled))
        {
            var predicted = EnsureComp<PredictedProjectileComponent>(uid);
            predicted.Shooter = user;
            predicted.PredictionId = gun.Comp.PredictionId;
            predicted.ProjectileIndex = projectileIndex;
            predicted.Origin = TransformSystem.GetMapCoordinates(uid);
            Dirty(uid, predicted);
        }

        ShootProjectile(uid, mapDirection, gunVelocity, gun, user, gun.Comp.ProjectileSpeedModified);
        return true;
    }

    // DS14-start
    private void ApplyExecutionShotDamage(EntityUid uid, Entity<GunComponent> gun)
    {
        if (!HasComp<GunExecutionShotComponent>(gun))
            return;

        const float executionDamageMultiplier = 9f;
        if (TryComp<ProjectileComponent>(uid, out var projectile))
        {
            projectile.Damage *= executionDamageMultiplier;
            projectile.IgnoreResistances = true;
        }

        if (TryComp<HitscanBasicDamageComponent>(uid, out var hitscan))
        {
            hitscan.Damage *= executionDamageMultiplier;
            hitscan.IgnoreResistances = true;
        }
    }
    // DS14-end

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

    protected override void Popup(string message, EntityUid? uid, EntityUid? user) { }

    protected override void CreateEffect(EntityUid gunUid, MuzzleFlashEvent message, EntityUid? user = null)
    {
        var filter = Filter.Pvs(gunUid, entityManager: EntityManager);

        if (TryComp<ActorComponent>(user, out var actor))
            filter.RemovePlayer(actor.PlayerSession);

        RaiseNetworkEvent(message, filter);
    }

    public override void PlayImpactSound(
        EntityUid otherEntity,
        DamageSpecifier? modifiedDamage,
        SoundSpecifier? weaponSound,
        bool forceWeaponSound,
        Filter? filter = null)
    {
        DebugTools.Assert(!Deleted(otherEntity), "Impact sound entity was deleted");
        filter ??= Filter.Pvs(otherEntity, entityManager: EntityManager);

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
                Audio.PlayEntity(damageSoundType, filter, otherEntity, true,
                    AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (type != null && rangedSound.SoundGroups?.TryGetValue(type, out var damageSoundGroup) == true)
            {
                Audio.PlayEntity(damageSoundGroup, filter, otherEntity, true,
                    AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
        }

        if (!playedSound && weaponSound != null)
        {
            Audio.PlayEntity(weaponSound, filter, otherEntity, true);
        }
    }
}
