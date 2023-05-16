using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Collections;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Melee;

public abstract class SharedMeleeWeaponSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] private   readonly IPrototypeManager _protoManager = default!;
    [Dependency] protected readonly ISharedAdminLogManager AdminLogger = default!;
    [Dependency] protected readonly ActionBlockerSystem Blocker = default!;
    [Dependency] protected readonly DamageableSystem Damageable = default!;
    [Dependency] private   readonly InventorySystem _inventory = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedCombatModeSystem CombatMode = default!;
    [Dependency] protected readonly SharedInteractionSystem Interaction = default!;
    [Dependency] private   readonly SharedPhysicsSystem _physics = default!;
    [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] private   readonly StaminaSystem _stamina = default!;

    protected ISawmill Sawmill = default!;

    public const float DamagePitchVariation = 0.05f;
    private const int AttackMask = (int) (CollisionGroup.MobMask | CollisionGroup.Opaque);

    /// <summary>
    /// Maximum amount of targets allowed for a wide-attack.
    /// </summary>
    public const int MaxTargets = 5;

    /// <summary>
    /// If an attack is released within this buffer it's assumed to be full damage.
    /// </summary>
    public const float GracePeriod = 0.05f;

    public override void Initialize()
    {
        base.Initialize();
        Sawmill = Logger.GetSawmill("melee");

        SubscribeLocalEvent<MeleeWeaponComponent, EntityUnpausedEvent>(OnMeleeUnpaused);
        SubscribeLocalEvent<MeleeWeaponComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<MeleeWeaponComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<MeleeWeaponComponent, HandDeselectedEvent>(OnMeleeDropped);
        SubscribeLocalEvent<MeleeWeaponComponent, HandSelectedEvent>(OnMeleeSelected);

        SubscribeAllEvent<HeavyAttackEvent>(OnHeavyAttack);
        SubscribeAllEvent<LightAttackEvent>(OnLightAttack);
        SubscribeAllEvent<StartHeavyAttackEvent>(OnStartHeavyAttack);
        SubscribeAllEvent<StopHeavyAttackEvent>(OnStopHeavyAttack);
        SubscribeAllEvent<DisarmAttackEvent>(OnDisarmAttack);
        SubscribeAllEvent<StopAttackEvent>(OnStopAttack);

#if DEBUG
        SubscribeLocalEvent<MeleeWeaponComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, MeleeWeaponComponent component, MapInitEvent args)
    {
        if (component.NextAttack > Timing.CurTime)
            Logger.Warning($"Initializing a map that contains an entity that is on cooldown. Entity: {ToPrettyString(uid)}");
#endif
    }

    private void OnMeleeUnpaused(EntityUid uid, MeleeWeaponComponent component, ref EntityUnpausedEvent args)
    {
        component.NextAttack += args.PausedTime;
    }

    private void OnMeleeSelected(EntityUid uid, MeleeWeaponComponent component, HandSelectedEvent args)
    {
        if (component.AttackRate.Equals(0f))
            return;

        if (!component.ResetOnHandSelected)
            return;

        if (Paused(uid))
            return;

        // If someone swaps to this weapon then reset its cd.
        var curTime = Timing.CurTime;
        var minimum = curTime + TimeSpan.FromSeconds(1 / component.AttackRate);

        if (minimum < component.NextAttack)
            return;

        component.NextAttack = minimum;
        Dirty(component);
    }

    private void OnMeleeDropped(EntityUid uid, MeleeWeaponComponent component, HandDeselectedEvent args)
    {
        if (component.WindUpStart == null)
            return;

        component.WindUpStart = null;
        Dirty(component);
    }

    private void OnStopAttack(StopAttackEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null)
            return;

        if (!TryGetWeapon(user.Value, out var weaponUid, out var weapon) ||
            weaponUid != msg.Weapon)
        {
            return;
        }

        if (!weapon.Attacking)
            return;

        weapon.Attacking = false;
        Dirty(weapon);
    }

    private void OnStartHeavyAttack(StartHeavyAttackEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null)
            return;

        if (!TryGetWeapon(user.Value, out var weaponUid, out var weapon) ||
            weaponUid != msg.Weapon)
        {
            return;
        }

        DebugTools.Assert(weapon.WindUpStart == null);
        weapon.WindUpStart = Timing.CurTime;
        Dirty(weapon);
    }

    protected abstract void Popup(string message, EntityUid? uid, EntityUid? user);

    private void OnLightAttack(LightAttackEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null)
            return;

        if (!TryGetWeapon(user.Value, out var weaponUid, out var weapon) ||
            weaponUid != msg.Weapon)
        {
            return;
        }

        AttemptAttack(args.SenderSession.AttachedEntity!.Value, msg.Weapon, weapon, msg, args.SenderSession);
    }

    private void OnStopHeavyAttack(StopHeavyAttackEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity == null)
        {
            return;
        }

        if (!TryGetWeapon(args.SenderSession.AttachedEntity.Value, out var weaponUid, out var weapon) ||
            weaponUid != msg.Weapon)
        {
            return;
        }

        if (weapon.WindUpStart.Equals(null))
        {
            return;
        }

        weapon.WindUpStart = null;
        Dirty(weapon);
    }

    private void OnHeavyAttack(HeavyAttackEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity == null)
        {
            return;
        }

        if (!TryGetWeapon(args.SenderSession.AttachedEntity.Value, out var weaponUid, out var weapon) ||
            weaponUid != msg.Weapon)
        {
            return;
        }

        AttemptAttack(args.SenderSession.AttachedEntity.Value, msg.Weapon, weapon, msg, args.SenderSession);
    }

    private void OnDisarmAttack(DisarmAttackEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity == null)
        {
            return;
        }

        if (!TryGetWeapon(args.SenderSession.AttachedEntity.Value, out var weaponUid, out var weapon))
        {
            return;
        }

        AttemptAttack(args.SenderSession.AttachedEntity.Value, weaponUid, weapon, msg, args.SenderSession);
    }

    private void OnGetState(EntityUid uid, MeleeWeaponComponent component, ref ComponentGetState args)
    {
        args.State = new MeleeWeaponComponentState(component.AttackRate, component.Attacking, component.NextAttack,
            component.WindUpStart, component.ClickAnimation, component.WideAnimation, component.Range);
    }

    private void OnHandleState(EntityUid uid, MeleeWeaponComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MeleeWeaponComponentState state)
            return;

        component.Attacking = state.Attacking;
        component.AttackRate = state.AttackRate;
        component.NextAttack = state.NextAttack;
        component.WindUpStart = state.WindUpStart;

        component.ClickAnimation = state.ClickAnimation;
        component.WideAnimation = state.WideAnimation;
        component.Range = state.Range;
    }

    public bool TryGetWeapon(EntityUid entity, out EntityUid weaponUid, [NotNullWhen(true)] out MeleeWeaponComponent? melee)
    {
        weaponUid = default;
        melee = null;

        var ev = new GetMeleeWeaponEvent();
        RaiseLocalEvent(entity, ev);
        if (ev.Handled)
        {
            if (TryComp(ev.Weapon, out melee))
            {
                weaponUid = ev.Weapon.Value;
                return true;
            }

            return false;
        }

        // Use inhands entity if we got one.
        if (EntityManager.TryGetComponent(entity, out HandsComponent? hands) &&
            hands.ActiveHandEntity is { } held)
        {
            if (EntityManager.TryGetComponent(held, out melee))
            {
                weaponUid = held;
                return true;
            }

            return false;
        }

        // Use hands clothing if applicable.
        if (_inventory.TryGetSlotEntity(entity, "gloves", out var gloves) &&
            TryComp<MeleeWeaponComponent>(gloves, out var glovesMelee))
        {
            weaponUid = gloves.Value;
            melee = glovesMelee;
            return true;
        }

        // Use our own melee
        if (TryComp(entity, out melee))
        {
            weaponUid = entity;
            return true;
        }

        return false;
    }

    public void AttemptLightAttackMiss(EntityUid user, EntityUid weaponUid, MeleeWeaponComponent weapon, EntityCoordinates coordinates)
    {
        AttemptAttack(user, weaponUid, weapon, new LightAttackEvent(null, weaponUid, coordinates), null);
    }

    public void AttemptLightAttack(EntityUid user, EntityUid weaponUid, MeleeWeaponComponent weapon, EntityUid target)
    {
        if (!TryComp<TransformComponent>(target, out var targetXform))
            return;

        AttemptAttack(user, weaponUid, weapon, new LightAttackEvent(target, weaponUid, targetXform.Coordinates), null);
    }

    public void AttemptDisarmAttack(EntityUid user, EntityUid weaponUid, MeleeWeaponComponent weapon, EntityUid target)
    {
        if (!TryComp<TransformComponent>(target, out var targetXform))
            return;

        AttemptAttack(user, weaponUid, weapon, new DisarmAttackEvent(target, targetXform.Coordinates), null);
    }

    /// <summary>
    /// Called when a windup is finished and an attack is tried.
    /// </summary>
    private void AttemptAttack(EntityUid user, EntityUid weaponUid, MeleeWeaponComponent weapon, AttackEvent attack, ICommonSession? session)
    {
        var curTime = Timing.CurTime;

        if (weapon.NextAttack > curTime)
            return;

        if (!CombatMode.IsInCombatMode(user))
            return;

        switch (attack)
        {
            case LightAttackEvent light:
                if (!Blocker.CanAttack(user, light.Target))
                    return;
                break;
            case DisarmAttackEvent disarm:
                if (!Blocker.CanAttack(user, disarm.Target))
                    return;
                break;
            default:
                if (!Blocker.CanAttack(user))
                    return;
                break;
        }

        // Windup time checked elsewhere.

        if (weapon.NextAttack < curTime)
            weapon.NextAttack = curTime;

        weapon.NextAttack += TimeSpan.FromSeconds(1f / weapon.AttackRate);

        // Attack confirmed
        string animation;

        switch (attack)
        {
            case LightAttackEvent light:
                DoLightAttack(user, light, weaponUid, weapon, session);
                animation = weapon.ClickAnimation;
                break;
            case DisarmAttackEvent disarm:
                if (!DoDisarm(user, disarm, weaponUid, weapon, session))
                    return;

                animation = weapon.ClickAnimation;
                break;
            case HeavyAttackEvent heavy:
                DoHeavyAttack(user, heavy, weaponUid, weapon, session);
                animation = weapon.WideAnimation;
                break;
            default:
                throw new NotImplementedException();
        }

        DoLungeAnimation(user, weapon.Angle, attack.Coordinates.ToMap(EntityManager, TransformSystem), weapon.Range, animation);
        weapon.Attacking = true;
        Dirty(weapon);
    }

    /// <summary>
    /// When an attack is released get the actual modifier for damage done.
    /// </summary>
    public float GetModifier(MeleeWeaponComponent component, bool lightAttack)
    {
        if (lightAttack)
            return 1f;

        var windup = component.WindUpStart;
        if (windup == null)
            return 0f;

        var releaseTime = (Timing.CurTime - windup.Value).TotalSeconds;
        var windupTime = component.WindupTime.TotalSeconds;

        // Wraps around back to 0
        releaseTime %= (2 * windupTime);

        var releaseDiff = Math.Abs(releaseTime - windupTime);

        if (releaseDiff < 0)
            releaseDiff = Math.Min(0, releaseDiff + GracePeriod);
        else
            releaseDiff = Math.Max(0, releaseDiff - GracePeriod);

        var fraction = (windupTime - releaseDiff) / windupTime;

        if (fraction < 0.4)
            fraction = 0;

        DebugTools.Assert(fraction <= 1);
        return (float) fraction * component.HeavyDamageModifier.Float();
    }

    protected abstract bool InRange(EntityUid user, EntityUid target, float range, ICommonSession? session);

    protected virtual void DoLightAttack(EntityUid user, LightAttackEvent ev, EntityUid meleeUid, MeleeWeaponComponent component, ICommonSession? session)
    {
        var damage = component.Damage * GetModifier(component, true);

        // For consistency with wide attacks stuff needs damageable.
        if (Deleted(ev.Target) ||
            !HasComp<DamageableComponent>(ev.Target) ||
            !TryComp<TransformComponent>(ev.Target, out var targetXform) ||
            // Not in LOS.
            !InRange(user, ev.Target.Value, component.Range, session))
        {
            // Leave IsHit set to true, because the only time it's set to false
            // is when a melee weapon is examined. Misses are inferred from an
            // empty HitEntities.
            // TODO: This needs fixing
            var missEvent = new MeleeHitEvent(new List<EntityUid>(), user, meleeUid, damage);
            RaiseLocalEvent(meleeUid, missEvent);
            Audio.PlayPredicted(component.SwingSound, meleeUid, user);
            return;
        }

        // Sawmill.Debug($"Melee damage is {damage.Total} out of {component.Damage.Total}");

        // Raise event before doing damage so we can cancel damage if the event is handled
        var hitEvent = new MeleeHitEvent(new List<EntityUid> { ev.Target.Value }, user, meleeUid, damage);
        RaiseLocalEvent(meleeUid, hitEvent);

        if (hitEvent.Handled)
            return;

        var targets = new List<EntityUid>(1)
        {
            ev.Target.Value
        };

        Interaction.DoContactInteraction(ev.Weapon, ev.Target);
        Interaction.DoContactInteraction(user, ev.Weapon);

        // If the user is using a long-range weapon, this probably shouldn't be happening? But I'll interpret melee as a
        // somewhat messy scuffle. See also, heavy attacks.
        Interaction.DoContactInteraction(user, ev.Target);

        // For stuff that cares about it being attacked.
        RaiseLocalEvent(ev.Target.Value, new AttackedEvent(meleeUid, user, targetXform.Coordinates));

        var modifiedDamage = DamageSpecifier.ApplyModifierSets(damage + hitEvent.BonusDamage, hitEvent.ModifiersList);
        var damageResult = Damageable.TryChangeDamage(ev.Target, modifiedDamage, origin:user);

        if (damageResult != null && damageResult.Total > FixedPoint2.Zero)
        {
            // If the target has stamina and is taking blunt damage, they should also take stamina damage based on their blunt to stamina factor
            if (damageResult.DamageDict.TryGetValue("Blunt", out var bluntDamage))
            {
                _stamina.TakeStaminaDamage(ev.Target.Value, (bluntDamage * component.BluntStaminaDamageFactor).Float(), source:user, with:meleeUid == user ? null : meleeUid);
            }

            if (meleeUid == user)
            {
                AdminLogger.Add(LogType.MeleeHit,
                    $"{ToPrettyString(user):user} melee attacked {ToPrettyString(ev.Target.Value):target} using their hands and dealt {damageResult.Total:damage} damage");
            }
            else
            {
                AdminLogger.Add(LogType.MeleeHit,
                    $"{ToPrettyString(user):user} melee attacked {ToPrettyString(ev.Target.Value):target} using {ToPrettyString(meleeUid):used} and dealt {damageResult.Total:damage} damage");
            }

            PlayHitSound(ev.Target.Value, user, GetHighestDamageSound(modifiedDamage, _protoManager), hitEvent.HitSoundOverride, component.HitSound);
        }
        else
        {
            if (hitEvent.HitSoundOverride != null)
            {
                Audio.PlayPredicted(hitEvent.HitSoundOverride, meleeUid, user);
            }
            else if (component.Damage.Total.Equals(FixedPoint2.Zero) && component.HitSound != null)
            {
                Audio.PlayPredicted(component.HitSound, meleeUid, user);
            }
            else
            {
                Audio.PlayPredicted(component.NoDamageSound, meleeUid, user);
            }
        }

        if (damageResult?.Total > FixedPoint2.Zero)
        {
            DoDamageEffect(targets, user, targetXform);
        }
    }

    protected abstract void DoDamageEffect(List<EntityUid> targets, EntityUid? user,  TransformComponent targetXform);

    private void DoHeavyAttack(EntityUid user, HeavyAttackEvent ev, EntityUid meleeUid, MeleeWeaponComponent component, ICommonSession? session)
    {
        // TODO: This is copy-paste as fuck with DoPreciseAttack
        if (!TryComp<TransformComponent>(user, out var userXform))
            return;

        var targetMap = ev.Coordinates.ToMap(EntityManager, TransformSystem);

        if (targetMap.MapId != userXform.MapID)
            return;

        var userPos = TransformSystem.GetWorldPosition(userXform);
        var direction = targetMap.Position - userPos;
        var distance = Math.Min(component.Range, direction.Length);

        var damage = component.Damage * GetModifier(component, false);
        var entities = ev.Entities;

        if (entities.Count == 0)
        {
            var missEvent = new MeleeHitEvent(new List<EntityUid>(), user, meleeUid, damage);
            RaiseLocalEvent(meleeUid, missEvent);

            Audio.PlayPredicted(component.SwingSound, meleeUid, user);
            return;
        }

        // Naughty input
        if (entities.Count > MaxTargets)
        {
            entities.RemoveRange(MaxTargets, entities.Count - MaxTargets);
        }

        // Validate client
        for (var i = entities.Count - 1; i >= 0; i--)
        {
            if (ArcRaySuccessful(entities[i], userPos, direction.ToWorldAngle(), component.Angle, distance,
                    userXform.MapID, user, session))
            {
                continue;
            }

            // Bad input
            entities.RemoveAt(i);
        }

        var targets = new List<EntityUid>();
        var damageQuery = GetEntityQuery<DamageableComponent>();

        foreach (var entity in entities)
        {
            if (entity == user ||
                !damageQuery.HasComponent(entity))
                continue;

            targets.Add(entity);
        }

        // Sawmill.Debug($"Melee damage is {damage.Total} out of {component.Damage.Total}");

        // Raise event before doing damage so we can cancel damage if the event is handled
        var hitEvent = new MeleeHitEvent(targets, user, meleeUid, damage);
        RaiseLocalEvent(meleeUid, hitEvent);

        if (hitEvent.Handled)
            return;

        Interaction.DoContactInteraction(user, ev.Weapon);

        // For stuff that cares about it being attacked.
        foreach (var target in targets)
        {
            Interaction.DoContactInteraction(ev.Weapon, target);

            // If the user is using a long-range weapon, this probably shouldn't be happening? But I'll interpret melee as a
            // somewhat messy scuffle. See also, light attacks.
            Interaction.DoContactInteraction(user, target);

            RaiseLocalEvent(target, new AttackedEvent(meleeUid, user, Transform(target).Coordinates));
        }

        var modifiedDamage = DamageSpecifier.ApplyModifierSets(damage + hitEvent.BonusDamage, hitEvent.ModifiersList);
        var appliedDamage = new DamageSpecifier();

        foreach (var entity in targets)
        {
            RaiseLocalEvent(entity, new AttackedEvent(meleeUid, user, ev.Coordinates));

            var damageResult = Damageable.TryChangeDamage(entity, modifiedDamage, origin:user);

            if (damageResult != null && damageResult.Total > FixedPoint2.Zero)
            {
                appliedDamage += damageResult;

                if (meleeUid == user)
                {
                    AdminLogger.Add(LogType.MeleeHit,
                        $"{ToPrettyString(user):user} melee attacked {ToPrettyString(entity):target} using their hands and dealt {damageResult.Total:damage} damage");
                }
                else
                {
                    AdminLogger.Add(LogType.MeleeHit,
                        $"{ToPrettyString(user):user} melee attacked {ToPrettyString(entity):target} using {ToPrettyString(meleeUid):used} and dealt {damageResult.Total:damage} damage");
                }
            }
        }

        if (entities.Count != 0)
        {
            if (appliedDamage.Total > FixedPoint2.Zero)
            {
                var target = entities.First();
                PlayHitSound(target, user, GetHighestDamageSound(modifiedDamage, _protoManager), hitEvent.HitSoundOverride, component.HitSound);
            }
            else
            {
                if (hitEvent.HitSoundOverride != null)
                {
                    Audio.PlayPredicted(hitEvent.HitSoundOverride, meleeUid, user);
                }
                else
                {
                    Audio.PlayPredicted(component.NoDamageSound, meleeUid, user);
                }
            }
        }

        if (appliedDamage.Total > FixedPoint2.Zero)
        {
            DoDamageEffect(targets, user, Transform(targets[0]));
        }
    }

    protected HashSet<EntityUid> ArcRayCast(Vector2 position, Angle angle, Angle arcWidth, float range, MapId mapId, EntityUid ignore)
    {
        // TODO: This is pretty sucky.
        var widthRad = arcWidth;
        var increments = 1 + 35 * (int) Math.Ceiling(widthRad / (2 * Math.PI));
        var increment = widthRad / increments;
        var baseAngle = angle - widthRad / 2;

        var resSet = new HashSet<EntityUid>();

        for (var i = 0; i < increments; i++)
        {
            var castAngle = new Angle(baseAngle + increment * i);
            var res = _physics.IntersectRay(mapId,
                new CollisionRay(position, castAngle.ToWorldVec(),
                    AttackMask), range, ignore, false).ToList();

            if (res.Count != 0)
            {
                resSet.Add(res[0].HitEntity);
            }
        }

        return resSet;
    }

    protected virtual bool ArcRaySuccessful(EntityUid targetUid, Vector2 position, Angle angle, Angle arcWidth, float range,
        MapId mapId, EntityUid ignore, ICommonSession? session)
    {
        // Only matters for server.
        return true;
    }

    private void PlayHitSound(EntityUid target, EntityUid? user, string? type, SoundSpecifier? hitSoundOverride, SoundSpecifier? hitSound)
    {
        var playedSound = false;

        // Play sound based off of highest damage type.
        if (TryComp<MeleeSoundComponent>(target, out var damageSoundComp))
        {
            if (type == null && damageSoundComp.NoDamageSound != null)
            {
                Audio.PlayPredicted(damageSoundComp.NoDamageSound, target, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (type != null && damageSoundComp.SoundTypes?.TryGetValue(type, out var damageSoundType) == true)
            {
                Audio.PlayPredicted(damageSoundType, target, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (type != null && damageSoundComp.SoundGroups?.TryGetValue(type, out var damageSoundGroup) == true)
            {
                Audio.PlayPredicted(damageSoundGroup, target, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
        }

        // Use weapon sounds if the thing being hit doesn't specify its own sounds.
        if (!playedSound)
        {
            if (hitSoundOverride != null)
            {
                Audio.PlayPredicted(hitSoundOverride, target, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
            else if (hitSound != null)
            {
                Audio.PlayPredicted(hitSound, target, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                playedSound = true;
            }
        }

        // Fallback to generic sounds.
        if (!playedSound)
        {
            switch (type)
            {
                // Unfortunately heat returns caustic group so can't just use the damagegroup in that instance.
                case "Burn":
                case "Heat":
                case "Cold":
                    Audio.PlayPredicted(new SoundPathSpecifier("/Audio/Items/welder.ogg"), target, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                    break;
                // No damage, fallback to tappies
                case null:
                    Audio.PlayPredicted(new SoundPathSpecifier("/Audio/Weapons/tap.ogg"), target, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                    break;
                case "Brute":
                    Audio.PlayPredicted(new SoundPathSpecifier("/Audio/Weapons/smash.ogg"), target, user, AudioParams.Default.WithVariation(DamagePitchVariation));
                    break;
            }
        }
    }

    public static string? GetHighestDamageSound(DamageSpecifier modifiedDamage, IPrototypeManager protoManager)
    {
        var groups = modifiedDamage.GetDamagePerGroup(protoManager);

        // Use group if it's exclusive, otherwise fall back to type.
        if (groups.Count == 1)
        {
            return groups.Keys.First();
        }

        var highestDamage = FixedPoint2.Zero;
        string? highestDamageType = null;

        foreach (var (type, damage) in modifiedDamage.DamageDict)
        {
            if (damage <= highestDamage)
                continue;

            highestDamageType = type;
        }

        return highestDamageType;
    }

    protected virtual bool DoDisarm(EntityUid user, DisarmAttackEvent ev, EntityUid meleeUid, MeleeWeaponComponent component, ICommonSession? session)
    {
        if (Deleted(ev.Target) ||
            user == ev.Target)
            return false;

        // Play a sound to give instant feedback; same with playing the animations
        Audio.PlayPredicted(component.SwingSound, meleeUid, user);
        return true;
    }

    private void DoLungeAnimation(EntityUid user, Angle angle, MapCoordinates coordinates, float length, string? animation)
    {
        // TODO: Assert that offset eyes are still okay.
        if (!TryComp<TransformComponent>(user, out var userXform))
            return;

        var invMatrix = TransformSystem.GetInvWorldMatrix(userXform);
        var localPos = invMatrix.Transform(coordinates.Position);

        if (localPos.LengthSquared <= 0f)
            return;

        localPos = userXform.LocalRotation.RotateVec(localPos);

        // We'll play the effect just short visually so it doesn't look like we should be hitting but actually aren't.
        const float bufferLength = 0.2f;
        var visualLength = length - bufferLength;

        if (localPos.Length > visualLength)
            localPos = localPos.Normalized * visualLength;

        DoLunge(user, angle, localPos, animation);
    }

    public abstract void DoLunge(EntityUid user, Angle angle, Vector2 localPos, string? animation, bool predicted = true);
}
