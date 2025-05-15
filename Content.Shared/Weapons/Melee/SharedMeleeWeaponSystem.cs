using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions.Events;
using Content.Shared.Administration.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using ItemToggleMeleeWeaponComponent = Content.Shared.Item.ItemToggle.Components.ItemToggleMeleeWeaponComponent;

namespace Content.Shared.Weapons.Melee;

public abstract class SharedMeleeWeaponSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] private   readonly INetManager _netMan = default!;
    [Dependency] private   readonly IPrototypeManager _protoManager = default!;
    [Dependency] private   readonly IRobustRandom _random = default!;
    [Dependency] protected readonly ISharedAdminLogManager AdminLogger = default!;
    [Dependency] protected readonly ActionBlockerSystem Blocker = default!;
    [Dependency] protected readonly DamageableSystem Damageable = default!;
    [Dependency] private   readonly InventorySystem _inventory = default!;
    [Dependency] private   readonly MeleeSoundSystem _meleeSound = default!;
    [Dependency] protected readonly MobStateSystem MobState = default!;
    [Dependency] private   readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly SharedCombatModeSystem CombatMode = default!;
    [Dependency] protected readonly SharedInteractionSystem Interaction = default!;
    [Dependency] private   readonly SharedPhysicsSystem _physics = default!;
    [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] private   readonly SharedStaminaSystem _stamina = default!;

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

        SubscribeLocalEvent<MeleeWeaponComponent, HandSelectedEvent>(OnMeleeSelected);
        SubscribeLocalEvent<MeleeWeaponComponent, ShotAttemptedEvent>(OnMeleeShotAttempted);
        SubscribeLocalEvent<MeleeWeaponComponent, GunShotEvent>(OnMeleeShot);
        SubscribeLocalEvent<BonusMeleeDamageComponent, GetMeleeDamageEvent>(OnGetBonusMeleeDamage);
        SubscribeLocalEvent<BonusMeleeDamageComponent, GetHeavyDamageModifierEvent>(OnGetBonusHeavyDamageModifier);
        SubscribeLocalEvent<BonusMeleeAttackRateComponent, GetMeleeAttackRateEvent>(OnGetBonusMeleeAttackRate);

        SubscribeLocalEvent<ItemToggleMeleeWeaponComponent, ItemToggledEvent>(OnItemToggle);

        SubscribeAllEvent<HeavyAttackEvent>(OnHeavyAttack);
        SubscribeAllEvent<LightAttackEvent>(OnLightAttack);
        SubscribeAllEvent<DisarmAttackEvent>(OnDisarmAttack);
        SubscribeAllEvent<StopAttackEvent>(OnStopAttack);

#if DEBUG
        SubscribeLocalEvent<MeleeWeaponComponent,
                            MapInitEvent>                   (OnMapInit);
    }

    private void OnMapInit(EntityUid uid, MeleeWeaponComponent component, MapInitEvent args)
    {
        if (component.NextAttack > Timing.CurTime)
            Log.Warning($"Initializing a map that contains an entity that is on cooldown. Entity: {ToPrettyString(uid)}");
#endif
    }

    private void OnMeleeShotAttempted(EntityUid uid, MeleeWeaponComponent comp, ref ShotAttemptedEvent args)
    {
        if (comp.NextAttack > Timing.CurTime)
            args.Cancel();
    }

    private void OnMeleeShot(EntityUid uid, MeleeWeaponComponent component, ref GunShotEvent args)
    {
        if (!TryComp<GunComponent>(uid, out var gun))
            return;

        if (gun.NextFire > component.NextAttack)
        {
            component.NextAttack = gun.NextFire;
            DirtyField(uid, component, nameof(MeleeWeaponComponent.NextAttack));
        }
    }

    private void OnMeleeSelected(EntityUid uid, MeleeWeaponComponent component, HandSelectedEvent args)
    {
        var attackRate = GetAttackRate(uid, args.User, component);
        if (attackRate.Equals(0f))
            return;

        if (!component.ResetOnHandSelected)
            return;

        if (Paused(uid))
            return;

        // If someone swaps to this weapon then reset its cd.
        var curTime = Timing.CurTime;
        var minimum = curTime + TimeSpan.FromSeconds(1 / attackRate);

        if (minimum < component.NextAttack)
            return;

        component.NextAttack = minimum;
        DirtyField(uid, component, nameof(MeleeWeaponComponent.NextAttack));
    }

    private void OnGetBonusMeleeDamage(EntityUid uid, BonusMeleeDamageComponent component, ref GetMeleeDamageEvent args)
    {
        if (component.BonusDamage != null)
            args.Damage += component.BonusDamage;
        if (component.DamageModifierSet != null)
            args.Modifiers.Add(component.DamageModifierSet);
    }

    private void OnGetBonusHeavyDamageModifier(EntityUid uid, BonusMeleeDamageComponent component, ref GetHeavyDamageModifierEvent args)
    {
        args.DamageModifier += component.HeavyDamageFlatModifier;
        args.Multipliers *= component.HeavyDamageMultiplier;
    }

    private void OnGetBonusMeleeAttackRate(EntityUid uid, BonusMeleeAttackRateComponent component, ref GetMeleeAttackRateEvent args)
    {
        args.Rate += component.FlatModifier;
        args.Multipliers *= component.Multiplier;
    }

    private void OnStopAttack(StopAttackEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null)
            return;

        if (!TryGetWeapon(user.Value, out var weaponUid, out var weapon) ||
            weaponUid != GetEntity(msg.Weapon))
        {
            return;
        }

        if (!weapon.Attacking)
            return;

        weapon.Attacking = false;
        DirtyField(weaponUid, weapon, nameof(MeleeWeaponComponent.Attacking));
    }

    private void OnLightAttack(LightAttackEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {} user)
            return;

        if (!TryGetWeapon(user, out var weaponUid, out var weapon) ||
            weaponUid != GetEntity(msg.Weapon))
        {
            return;
        }

        AttemptAttack(user, weaponUid, weapon, msg, args.SenderSession);
    }

    private void OnHeavyAttack(HeavyAttackEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {} user)
            return;

        if (!TryGetWeapon(user, out var weaponUid, out var weapon) ||
            weaponUid != GetEntity(msg.Weapon))
        {
            return;
        }

        AttemptAttack(user, weaponUid, weapon, msg, args.SenderSession);
    }

    private void OnDisarmAttack(DisarmAttackEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {} user)
            return;

        if (TryGetWeapon(user, out var weaponUid, out var weapon))
            AttemptAttack(user, weaponUid, weapon, msg, args.SenderSession);
    }

    /// <summary>
    /// Gets the total damage a weapon does, including modifiers like wielding and enablind/disabling
    /// </summary>
    public DamageSpecifier GetDamage(EntityUid uid, EntityUid user, MeleeWeaponComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return new DamageSpecifier();

        var ev = new GetMeleeDamageEvent(uid, new(component.Damage * Damageable.UniversalMeleeDamageModifier), new(), user, component.ResistanceBypass);
        RaiseLocalEvent(uid, ref ev);

        return DamageSpecifier.ApplyModifierSets(ev.Damage, ev.Modifiers);
    }

    public float GetAttackRate(EntityUid uid, EntityUid user, MeleeWeaponComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return 0;

        var ev = new GetMeleeAttackRateEvent(uid, component.AttackRate, 1, user);
        RaiseLocalEvent(uid, ref ev);

        return ev.Rate * ev.Multipliers;
    }

    public FixedPoint2 GetHeavyDamageModifier(EntityUid uid, EntityUid user, MeleeWeaponComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return FixedPoint2.Zero;

        var ev = new GetHeavyDamageModifierEvent(uid, component.ClickDamageModifier, 1, user);
        RaiseLocalEvent(uid, ref ev);

        return ev.DamageModifier * ev.Multipliers;
    }

    public bool GetResistanceBypass(EntityUid uid, EntityUid user, MeleeWeaponComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        var ev = new GetMeleeDamageEvent(uid, new(component.Damage * Damageable.UniversalMeleeDamageModifier), new(), user, component.ResistanceBypass);
        RaiseLocalEvent(uid, ref ev);

        return ev.ResistanceBypass;
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
            // Make sure the entity is a weapon AND it doesn't need
            // to be equipped to be used (E.g boxing gloves).
            if (EntityManager.TryGetComponent(held, out melee) &&
                !melee.MustBeEquippedToUse)
            {
                weaponUid = held;
                return true;
            }

            if (!HasComp<VirtualItemComponent>(held))
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
        AttemptAttack(user, weaponUid, weapon, new LightAttackEvent(null, GetNetEntity(weaponUid), GetNetCoordinates(coordinates)), null);
    }

    public bool AttemptLightAttack(EntityUid user, EntityUid weaponUid, MeleeWeaponComponent weapon, EntityUid target)
    {
        if (!TryComp(target, out TransformComponent? targetXform))
            return false;

        return AttemptAttack(user, weaponUid, weapon, new LightAttackEvent(GetNetEntity(target), GetNetEntity(weaponUid), GetNetCoordinates(targetXform.Coordinates)), null);
    }

    public bool AttemptDisarmAttack(EntityUid user, EntityUid weaponUid, MeleeWeaponComponent weapon, EntityUid target)
    {
        if (!TryComp(target, out TransformComponent? targetXform))
            return false;

        return AttemptAttack(user, weaponUid, weapon, new DisarmAttackEvent(GetNetEntity(target), GetNetCoordinates(targetXform.Coordinates)), null);
    }

    /// <summary>
    /// Called when a windup is finished and an attack is tried.
    /// </summary>
    /// <returns>True if attack successful</returns>
    private bool AttemptAttack(EntityUid user, EntityUid weaponUid, MeleeWeaponComponent weapon, AttackEvent attack, ICommonSession? session)
    {
        var curTime = Timing.CurTime;

        if (weapon.NextAttack > curTime)
            return false;

        if (!CombatMode.IsInCombatMode(user))
            return false;

        EntityUid? target = null;
        switch (attack)
        {
            case LightAttackEvent light:
                if (light.Target != null && !TryGetEntity(light.Target, out target))
                {
                    // Target was lightly attacked & deleted.
                    return false;
                }

                if (!Blocker.CanAttack(user, target, (weaponUid, weapon)))
                    return false;

                // Can't self-attack if you're the weapon
                if (weaponUid == target)
                    return false;

                break;
            case DisarmAttackEvent disarm:
                if (disarm.Target != null && !TryGetEntity(disarm.Target, out target))
                {
                    // Target was lightly attacked & deleted.
                    return false;
                }

                if (!Blocker.CanAttack(user, target, (weaponUid, weapon), true))
                    return false;
                break;
            default:
                if (!Blocker.CanAttack(user, weapon: (weaponUid, weapon)))
                    return false;
                break;
        }

        // Windup time checked elsewhere.
        var fireRate = TimeSpan.FromSeconds(1f / GetAttackRate(weaponUid, user, weapon));
        var swings = 0;

        // TODO: If we get autoattacks then probably need a shotcounter like guns so we can do timing properly.
        if (weapon.NextAttack < curTime)
            weapon.NextAttack = curTime;

        while (weapon.NextAttack <= curTime)
        {
            weapon.NextAttack += fireRate;
            swings++;
        }

        DirtyField(weaponUid, weapon, nameof(MeleeWeaponComponent.NextAttack));

        // Do this AFTER attack so it doesn't spam every tick
        var ev = new AttemptMeleeEvent();
        RaiseLocalEvent(weaponUid, ref ev);

        if (ev.Cancelled)
        {
            if (ev.Message != null)
            {
                PopupSystem.PopupClient(ev.Message, weaponUid, user);
            }

            return false;
        }

        // Attack confirmed
        for (var i = 0; i < swings; i++)
        {
            string animation;

            switch (attack)
            {
                case LightAttackEvent light:
                    DoLightAttack(user, light, weaponUid, weapon, session);
                    animation = weapon.Animation;
                    break;
                case DisarmAttackEvent disarm:
                    if (!DoDisarm(user, disarm, weaponUid, weapon, session))
                        return false;

                    animation = weapon.Animation;
                    break;
                case HeavyAttackEvent heavy:
                    if (!DoHeavyAttack(user, heavy, weaponUid, weapon, session))
                        return false;

                    animation = weapon.WideAnimation;
                    break;
                default:
                    throw new NotImplementedException();
            }

            DoLungeAnimation(user, weaponUid, weapon.Angle, TransformSystem.ToMapCoordinates(GetCoordinates(attack.Coordinates)), weapon.Range, animation);
        }

        var attackEv = new MeleeAttackEvent(weaponUid);
        RaiseLocalEvent(user, ref attackEv);

        weapon.Attacking = true;
        DirtyField(weaponUid, weapon, nameof(MeleeWeaponComponent.Attacking));
        return true;
    }

    protected abstract bool InRange(EntityUid user, EntityUid target, float range, ICommonSession? session);

    protected virtual void DoLightAttack(EntityUid user, LightAttackEvent ev, EntityUid meleeUid, MeleeWeaponComponent component, ICommonSession? session)
    {
        // If I do not come back later to fix Light Attacks being Heavy Attacks you can throw me in the spider pit -Errant
        var damage = GetDamage(meleeUid, user, component) * GetHeavyDamageModifier(meleeUid, user, component);
        var target = GetEntity(ev.Target);
        var resistanceBypass = GetResistanceBypass(meleeUid, user, component);

        // For consistency with wide attacks stuff needs damageable.
        if (Deleted(target) ||
            !HasComp<DamageableComponent>(target) ||
            !TryComp(target, out TransformComponent? targetXform) ||
            // Not in LOS.
            !InRange(user, target.Value, component.Range, session))
        {
            // Leave IsHit set to true, because the only time it's set to false
            // is when a melee weapon is examined. Misses are inferred from an
            // empty HitEntities.
            // TODO: This needs fixing
            if (meleeUid == user)
            {
                AdminLogger.Add(LogType.MeleeHit,
                    LogImpact.Low,
                    $"{ToPrettyString(user):actor} melee attacked (light) using their hands and missed");
            }
            else
            {
                AdminLogger.Add(LogType.MeleeHit,
                    LogImpact.Low,
                    $"{ToPrettyString(user):actor} melee attacked (light) using {ToPrettyString(meleeUid):tool} and missed");
            }
            var missEvent = new MeleeHitEvent(new List<EntityUid>(), user, meleeUid, damage, null);
            RaiseLocalEvent(meleeUid, missEvent);
            _meleeSound.PlaySwingSound(user, meleeUid, component);
            return;
        }

        // Sawmill.Debug($"Melee damage is {damage.Total} out of {component.Damage.Total}");

        // Raise event before doing damage so we can cancel damage if the event is handled
        var hitEvent = new MeleeHitEvent(new List<EntityUid> { target.Value }, user, meleeUid, damage, null);
        RaiseLocalEvent(meleeUid, hitEvent);

        if (hitEvent.Handled)
            return;

        var targets = new List<EntityUid>(1)
        {
            target.Value
        };

        var weapon = GetEntity(ev.Weapon);

        // We skip weapon -> target interaction, as forensics system applies DNA on hit
        Interaction.DoContactInteraction(user, weapon);

        // If the user is using a long-range weapon, this probably shouldn't be happening? But I'll interpret melee as a
        // somewhat messy scuffle. See also, heavy attacks.
        Interaction.DoContactInteraction(user, target);

        // For stuff that cares about it being attacked.
        var attackedEvent = new AttackedEvent(meleeUid, user, targetXform.Coordinates);
        RaiseLocalEvent(target.Value, attackedEvent);

        var modifiedDamage = DamageSpecifier.ApplyModifierSets(damage + hitEvent.BonusDamage + attackedEvent.BonusDamage, hitEvent.ModifiersList);
        var damageResult = Damageable.TryChangeDamage(target, modifiedDamage, origin:user, ignoreResistances:resistanceBypass);

        if (damageResult is {Empty: false})
        {
            // If the target has stamina and is taking blunt damage, they should also take stamina damage based on their blunt to stamina factor
            if (damageResult.DamageDict.TryGetValue("Blunt", out var bluntDamage))
            {
                _stamina.TakeStaminaDamage(target.Value, (bluntDamage * component.BluntStaminaDamageFactor).Float(), visual: false, source: user, with: meleeUid == user ? null : meleeUid);
            }

            if (meleeUid == user)
            {
                AdminLogger.Add(LogType.MeleeHit,
                    LogImpact.Medium,
                    $"{ToPrettyString(user):actor} melee attacked (light) {ToPrettyString(target.Value):subject} using their hands and dealt {damageResult.GetTotal():damage} damage");
            }
            else
            {
                AdminLogger.Add(LogType.MeleeHit,
                    LogImpact.Medium,
                    $"{ToPrettyString(user):actor} melee attacked (light) {ToPrettyString(target.Value):subject} using {ToPrettyString(meleeUid):tool} and dealt {damageResult.GetTotal():damage} damage");
            }

        }

        _meleeSound.PlayHitSound(target.Value, user, GetHighestDamageSound(modifiedDamage, _protoManager), hitEvent.HitSoundOverride, component);

        if (damageResult?.GetTotal() > FixedPoint2.Zero)
        {
            DoDamageEffect(targets, user, targetXform);
        }
    }

    protected abstract void DoDamageEffect(List<EntityUid> targets, EntityUid? user,  TransformComponent targetXform);

    private bool DoHeavyAttack(EntityUid user, HeavyAttackEvent ev, EntityUid meleeUid, MeleeWeaponComponent component, ICommonSession? session)
    {
        // TODO: This is copy-paste as fuck with DoPreciseAttack
        if (!TryComp(user, out TransformComponent? userXform))
            return false;

        var targetMap = TransformSystem.ToMapCoordinates(GetCoordinates(ev.Coordinates));

        if (targetMap.MapId != userXform.MapID)
            return false;

        var userPos = TransformSystem.GetWorldPosition(userXform);
        var direction = targetMap.Position - userPos;
        var distance = Math.Min(component.Range, direction.Length());

        var damage = GetDamage(meleeUid, user, component);
        var entities = GetEntityList(ev.Entities);

        if (entities.Count == 0)
        {
            if (meleeUid == user)
            {
                AdminLogger.Add(LogType.MeleeHit,
                    LogImpact.Low,
                    $"{ToPrettyString(user):actor} melee attacked (heavy) using their hands and missed");
            }
            else
            {
                AdminLogger.Add(LogType.MeleeHit,
                    LogImpact.Low,
                    $"{ToPrettyString(user):actor} melee attacked (heavy) using {ToPrettyString(meleeUid):tool} and missed");
            }
            var missEvent = new MeleeHitEvent(new List<EntityUid>(), user, meleeUid, damage, direction);
            RaiseLocalEvent(meleeUid, missEvent);

            // immediate audio feedback
            _meleeSound.PlaySwingSound(user, meleeUid, component);

            return true;
        }

        // Naughty input
        if (entities.Count > MaxTargets)
        {
            entities.RemoveRange(MaxTargets, entities.Count - MaxTargets);
        }

        // Validate client
        for (var i = entities.Count - 1; i >= 0; i--)
        {
            if (ArcRaySuccessful(entities[i],
                    userPos,
                    direction.ToWorldAngle(),
                    component.Angle,
                    distance,
                    userXform.MapID,
                    user,
                    session))
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
        var hitEvent = new MeleeHitEvent(targets, user, meleeUid, damage, direction);
        RaiseLocalEvent(meleeUid, hitEvent);

        if (hitEvent.Handled)
            return true;

        var weapon = GetEntity(ev.Weapon);

        Interaction.DoContactInteraction(user, weapon);

        // For stuff that cares about it being attacked.
        foreach (var target in targets)
        {
            // We skip weapon -> target interaction, as forensics system applies DNA on hit

            // If the user is using a long-range weapon, this probably shouldn't be happening? But I'll interpret melee as a
            // somewhat messy scuffle. See also, light attacks.
            Interaction.DoContactInteraction(user, target);
        }

        var appliedDamage = new DamageSpecifier();

        for (var i = targets.Count - 1; i >= 0; i--)
        {
            var entity = targets[i];
            // We raise an attack attempt here as well,
            // primarily because this was an untargeted wideswing: if a subscriber to that event cared about
            // the potential target (such as for pacifism), they need to be made aware of the target here.
            // In that case, just continue.
            if (!Blocker.CanAttack(user, entity, (weapon, component)))
            {
                targets.RemoveAt(i);
                continue;
            }

            var attackedEvent = new AttackedEvent(meleeUid, user, GetCoordinates(ev.Coordinates));
            RaiseLocalEvent(entity, attackedEvent);
            var modifiedDamage = DamageSpecifier.ApplyModifierSets(damage + hitEvent.BonusDamage + attackedEvent.BonusDamage, hitEvent.ModifiersList);

            var damageResult = Damageable.TryChangeDamage(entity, modifiedDamage, origin:user);

            if (damageResult != null && damageResult.GetTotal() > FixedPoint2.Zero)
            {
                // If the target has stamina and is taking blunt damage, they should also take stamina damage based on their blunt to stamina factor
                if (damageResult.DamageDict.TryGetValue("Blunt", out var bluntDamage))
                {
                    _stamina.TakeStaminaDamage(entity, (bluntDamage * component.BluntStaminaDamageFactor).Float(), visual: false, source: user, with: meleeUid == user ? null : meleeUid);
                }

                appliedDamage += damageResult;

                if (meleeUid == user)
                {
                    AdminLogger.Add(LogType.MeleeHit,
                        LogImpact.Medium,
                        $"{ToPrettyString(user):actor} melee attacked (heavy) {ToPrettyString(entity):subject} using their hands and dealt {damageResult.GetTotal():damage} damage");
                }
                else
                {
                    AdminLogger.Add(LogType.MeleeHit,
                        LogImpact.Medium,
                        $"{ToPrettyString(user):actor} melee attacked (heavy) {ToPrettyString(entity):subject} using {ToPrettyString(meleeUid):tool} and dealt {damageResult.GetTotal():damage} damage");
                }
            }
        }

        if (entities.Count != 0)
        {
            var target = entities.First();
            _meleeSound.PlayHitSound(target, user, GetHighestDamageSound(appliedDamage, _protoManager), hitEvent.HitSoundOverride, component);
        }

        if (appliedDamage.GetTotal() > FixedPoint2.Zero)
        {
            DoDamageEffect(targets, user, Transform(targets[0]));
        }

        return true;
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
                new CollisionRay(position,
                    castAngle.ToWorldVec(),
                    AttackMask),
                range,
                ignore,
                false)
                .ToList();

            if (res.Count != 0)
            {
                // If there's exact distance overlap, we simply have to deal with all overlapping objects to avoid selecting randomly.
                var resChecked = res.Where(x => x.Distance.Equals(res[0].Distance));
                foreach (var r in resChecked)
                {
                    if (Interaction.InRangeUnobstructed(ignore, r.HitEntity, range + 0.1f, overlapCheck: false))
                        resSet.Add(r.HitEntity);
                }
            }
        }

        return resSet;
    }

    protected virtual bool ArcRaySuccessful(EntityUid targetUid,
        Vector2 position,
        Angle angle,
        Angle arcWidth,
        float range,
        MapId mapId,
        EntityUid ignore,
        ICommonSession? session)
    {
        // Only matters for server.
        return true;
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

    private float CalculateDisarmChance(EntityUid disarmer, EntityUid disarmed, EntityUid? inTargetHand, CombatModeComponent disarmerComp)
    {
        if (HasComp<DisarmProneComponent>(disarmer))
            return 1.0f;

        if (HasComp<DisarmProneComponent>(disarmed))
            return 0.0f;

        var chance = disarmerComp.BaseDisarmFailChance;

        if (inTargetHand != null && TryComp<DisarmMalusComponent>(inTargetHand, out var malus))
        {
            chance += malus.Malus;
        }

        return Math.Clamp(chance, 0f, 1f);
    }

    private bool DoDisarm(EntityUid user, DisarmAttackEvent ev, EntityUid meleeUid, MeleeWeaponComponent component, ICommonSession? session)
    {
        var target = GetEntity(ev.Target);

        if (Deleted(target) ||
            user == target)
        {
            return false;
        }


        if (MobState.IsIncapacitated(target.Value))
        {
            return false;
        }

        if (!TryComp<CombatModeComponent>(user, out var combatMode) ||
            combatMode.CanDisarm != true)
        {
            return false;
        }

        // Need hands or to be able to be shoved over.
        if (!TryComp<HandsComponent>(target, out var targetHandsComponent))
        {
            if (!TryComp<StatusEffectsComponent>(target, out var status) ||
                !status.AllowedEffects.Contains("KnockedDown"))
            {
                // Notify disarmable
                if (HasComp<MobStateComponent>(target.Value))
                    PopupSystem.PopupClient(Loc.GetString("disarm-action-disarmable", ("targetName", target.Value)), target.Value);

                return false;
            }
        }

        if (!InRange(user, target.Value, component.Range, session))
        {
            return false;
        }

        EntityUid? inTargetHand = null;

        if (targetHandsComponent?.ActiveHand is { IsEmpty: false })
        {
            inTargetHand = targetHandsComponent.ActiveHand.HeldEntity!.Value;
        }

        var attemptEvent = new DisarmAttemptEvent(target.Value, user, inTargetHand);

        if (inTargetHand != null)
        {
            RaiseLocalEvent(inTargetHand.Value, ref attemptEvent);
        }

        RaiseLocalEvent(target.Value, ref attemptEvent);

        if (attemptEvent.Cancelled)
            return false;

        var chance = CalculateDisarmChance(user, target.Value, inTargetHand, combatMode);

        // At this point we diverge
        if (_netMan.IsClient)
        {
            // Play a sound to give instant feedback; same with playing the animations
            _meleeSound.PlaySwingSound(user, meleeUid, component);
            return true;
        }

        if (_random.Prob(chance))
        {
            return false;
        }

        var eventArgs = new DisarmedEvent(target.Value, user, 1 - chance);
        RaiseLocalEvent(target.Value, ref eventArgs);

        // Nothing handled it so abort.
        if (!eventArgs.Handled)
        {
            return false;
        }

        Interaction.DoContactInteraction(user, target);
        AdminLogger.Add(LogType.DisarmedAction, $"{ToPrettyString(user):user} used disarm on {ToPrettyString(target):target}");

        AdminLogger.Add(LogType.DisarmedAction, $"{ToPrettyString(user):user} used disarm on {ToPrettyString(target):target}");

        _audio.PlayPvs(combatMode.DisarmSuccessSound, target.Value, AudioParams.Default.WithVariation(0.025f).WithVolume(5f));
        var targetEnt = Identity.Entity(target.Value, EntityManager);
        var userEnt = Identity.Entity(user, EntityManager);

        var msgOther = Loc.GetString(
            eventArgs.PopupPrefix + "popup-message-other-clients",
            ("performerName", userEnt),
            ("targetName", targetEnt));

        var msgUser = Loc.GetString(eventArgs.PopupPrefix + "popup-message-cursor", ("targetName", targetEnt));

        var filterOther = Filter.PvsExcept(user, entityManager: EntityManager);

        PopupSystem.PopupEntity(msgOther, user, filterOther, true);
        PopupSystem.PopupEntity(msgUser, target.Value, user);

        if (eventArgs.IsStunned)
        {

            PopupSystem.PopupEntity(Loc.GetString("stunned-component-disarm-success-others", ("source", userEnt), ("target", targetEnt)), targetEnt, Filter.PvsExcept(user), true, PopupType.LargeCaution);
            PopupSystem.PopupCursor(Loc.GetString("stunned-component-disarm-success", ("target", targetEnt)), user, PopupType.Large);

            AdminLogger.Add(LogType.DisarmedKnockdown, LogImpact.Medium, $"{ToPrettyString(user):user} knocked down {ToPrettyString(target):target}");
        }

        return true;
    }

    private void DoLungeAnimation(EntityUid user, EntityUid weapon, Angle angle, MapCoordinates coordinates, float length, string? animation)
    {
        // TODO: Assert that offset eyes are still okay.
        if (!TryComp(user, out TransformComponent? userXform))
            return;

        var invMatrix = TransformSystem.GetInvWorldMatrix(userXform);
        var localPos = Vector2.Transform(coordinates.Position, invMatrix);

        if (localPos.LengthSquared() <= 0f)
            return;

        localPos = userXform.LocalRotation.RotateVec(localPos);

        // We'll play the effect just short visually so it doesn't look like we should be hitting but actually aren't.
        const float bufferLength = 0.2f;
        var visualLength = length - bufferLength;

        if (localPos.Length() > visualLength)
            localPos = localPos.Normalized() * visualLength;

        DoLunge(user, weapon, angle, localPos, animation);
    }

    public abstract void DoLunge(EntityUid user, EntityUid weapon, Angle angle, Vector2 localPos, string? animation, bool predicted = true);

    /// <summary>
    /// Used to update the MeleeWeapon component on item toggle.
    /// </summary>
    private void OnItemToggle(EntityUid uid, ItemToggleMeleeWeaponComponent itemToggleMelee, ItemToggledEvent args)
    {
        if (!TryComp(uid, out MeleeWeaponComponent? meleeWeapon))
            return;

        if (args.Activated)
        {
            if (itemToggleMelee.ActivatedDamage != null)
            {
                //Setting deactivated damage to the weapon's regular value before changing it.
                itemToggleMelee.DeactivatedDamage ??= meleeWeapon.Damage;
                meleeWeapon.Damage = itemToggleMelee.ActivatedDamage;
                DirtyField(uid, meleeWeapon, nameof(MeleeWeaponComponent.Damage));
            }

            if (meleeWeapon.HitSound?.Equals(itemToggleMelee.ActivatedSoundOnHit) != true)
            {
                meleeWeapon.HitSound = itemToggleMelee.ActivatedSoundOnHit;
                DirtyField(uid, meleeWeapon, nameof(MeleeWeaponComponent.HitSound));
            }

            if (itemToggleMelee.ActivatedSoundOnHitNoDamage != null)
            {
                //Setting the deactivated sound on no damage hit to the weapon's regular value before changing it.
                itemToggleMelee.DeactivatedSoundOnHitNoDamage ??= meleeWeapon.NoDamageSound;
                meleeWeapon.NoDamageSound = itemToggleMelee.ActivatedSoundOnHitNoDamage;
                DirtyField(uid, meleeWeapon, nameof(MeleeWeaponComponent.NoDamageSound));
            }

            if (itemToggleMelee.ActivatedSoundOnSwing != null)
            {
                //Setting the deactivated sound on no damage hit to the weapon's regular value before changing it.
                itemToggleMelee.DeactivatedSoundOnSwing ??= meleeWeapon.SwingSound;
                meleeWeapon.SwingSound = itemToggleMelee.ActivatedSoundOnSwing;
                DirtyField(uid, meleeWeapon, nameof(MeleeWeaponComponent.SwingSound));
            }

            if (itemToggleMelee.DeactivatedSecret)
            {
                meleeWeapon.Hidden = false;
            }
        }
        else
        {
            if (itemToggleMelee.DeactivatedDamage != null)
            {
                meleeWeapon.Damage = itemToggleMelee.DeactivatedDamage;
                DirtyField(uid, meleeWeapon, nameof(MeleeWeaponComponent.Damage));
            }

            meleeWeapon.HitSound = itemToggleMelee.DeactivatedSoundOnHit;
            DirtyField(uid, meleeWeapon, nameof(MeleeWeaponComponent.HitSound));

            if (itemToggleMelee.DeactivatedSoundOnHitNoDamage != null)
            {
                meleeWeapon.NoDamageSound = itemToggleMelee.DeactivatedSoundOnHitNoDamage;
                DirtyField(uid, meleeWeapon, nameof(MeleeWeaponComponent.NoDamageSound));
            }

            if (itemToggleMelee.DeactivatedSoundOnSwing != null)
            {
                meleeWeapon.SwingSound = itemToggleMelee.DeactivatedSoundOnSwing;
                DirtyField(uid, meleeWeapon, nameof(MeleeWeaponComponent.SwingSound));
            }

            if (itemToggleMelee.DeactivatedSecret)
            {
                meleeWeapon.Hidden = true;
            }
        }
    }
}
