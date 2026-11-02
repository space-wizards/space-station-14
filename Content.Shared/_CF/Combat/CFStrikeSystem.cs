using System.Numerics;
using Content.Shared.CF.Actions;
using Content.Shared.CF.Character;
using Content.Shared.CF.Factions;
using Content.Shared.CF.Preferences;
using Content.Shared.CF.Spells;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.CF.Combat;

/// <summary>
/// Pathfinder 2e Strikes via stock SS14 melee and projectile hits.
/// </summary>
public sealed partial class CFStrikeSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedDestructibleSystem _destructible = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private CFCombatBridgeSystem _bridge = default!;
    [Dependency] private CFDyingSystem _dying = default!;
    [Dependency] private CFFlankingSystem _flanking = default!;
    [Dependency] private CFFactionSystem _factions = default!;
    [Dependency] private CFActionPoolSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    /// <summary>Unconscious / dying creatures are off-guard (−4 AC status, simplified).</summary>
    private const int DownedAcPenalty = 4;

    [SubscribeLocalEvent]
    private void OnCfWeaponHit(Entity<CFMeleeWeaponComponent> weapon, ref MeleeHitEvent args)
    {
        if (!TryGetMeleeAttacker(args, out var combat, out var abilities))
            return;

        args.Handled = true;
        if (!_net.IsServer)
            return;

        var stats = CFStrikeWeaponStats.FromMelee(weapon.Comp);
        if (args.HitEntities.Count == 0)
        {
            _actions.CountAttack(args.User);
            return;
        }

        BeginMeleeStrikes(args.User, args.HitEntities, combat, abilities, stats, weapon.Owner, melee: true);
    }

    [SubscribeLocalEvent]
    private void OnUnarmedMeleeHit(Entity<MeleeWeaponComponent> weapon, ref MeleeHitEvent args)
    {
        if (HasComp<CFMeleeWeaponComponent>(weapon))
            return;

        if (!IsUnarmedWeapon(args.User, weapon.Owner))
            return;

        if (!TryGetMeleeAttacker(args, out var combat, out var abilities))
            return;

        args.Handled = true;
        if (!_net.IsServer)
            return;

        if (args.HitEntities.Count == 0)
        {
            _actions.CountAttack(args.User);
            return;
        }

        BeginMeleeStrikes(args.User, args.HitEntities, combat, abilities, CFStrikeWeaponStats.Unarmed(), weapon.Owner, melee: true);
    }

    /// <summary>
    /// Projectile fired from a <see cref="CFRangedWeaponComponent"/> — replace SS14 damage with a PF2e Strike.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBeforeProjectileHit(Entity<ProjectileComponent> projectile, ref BeforeProjectileHitEvent args)
    {
        var shooter = args.Shooter ?? projectile.Comp.Shooter;
        if (shooter != null && !_factions.CanHarm(shooter.Value, args.Target))
        {
            args.Damage = new DamageSpecifier();
            return;
        }

        if (projectile.Comp.Weapon is not { } weaponUid
            || !TryComp(weaponUid, out CFRangedWeaponComponent? ranged))
            return;

        if (shooter == null
            || !TryComp(shooter.Value, out CFCombatantComponent? combat)
            || !TryComp(shooter.Value, out CFAbilityScoreComponent? abilities))
            return;

        // Cancel stock projectile damage (CF Strike applies its own mirrored SS14 damage).
        args.Damage = new DamageSpecifier();

        if (!_net.IsServer)
            return;

        var stats = CFStrikeWeaponStats.FromRanged(ranged);
        var distanceFeet = GetDistanceFeet(shooter.Value, args.Target);
        var map = _actions.MapPenalty(shooter.Value, stats.Agile);
        _actions.CountAttack(shooter.Value);
        ResolveStrike(
            shooter.Value,
            args.Target,
            combat,
            abilities,
            stats,
            distanceFeet,
            ranged.Propulsive,
            weaponUid,
            melee: false,
            map);
    }

    private bool TryGetMeleeAttacker(
        MeleeHitEvent args,
        out CFCombatantComponent combat,
        out CFAbilityScoreComponent abilities)
    {
        combat = default!;
        abilities = default!;

        if (!args.IsHit)
            return false;

        return TryComp(args.User, out combat!) && TryComp(args.User, out abilities!);
    }

    private bool IsUnarmedWeapon(EntityUid user, EntityUid weapon)
    {
        if (weapon == user)
            return true;

        if (_inventory.TryGetSlotEntity(user, "gloves", out var gloves) && gloves == weapon)
            return true;

        if (_hands.IsHolding(user, weapon))
            return false;

        return false;
    }

    private void BeginMeleeStrikes(
        EntityUid user,
        IReadOnlySet<EntityUid> targets,
        CFCombatantComponent combat,
        CFAbilityScoreComponent abilities,
        CFStrikeWeaponStats stats,
        EntityUid weaponUid,
        bool melee)
    {
        // One swing is one attack, even when the arc touches more than one creature.
        var map = _actions.MapPenalty(user, stats.Agile);
        var counted = false;
        foreach (var target in targets)
        {
            // Party members never reach AC, the d20, or damage.
            if (!_factions.CanHarm(user, target))
                continue;

            if (!counted)
            {
                _actions.CountAttack(user);
                counted = true;
            }

            ResolveStrike(user, target, combat, abilities, stats, distanceFeet: 0f, propulsive: false, weaponUid, melee, map);
        }
    }

    private void ResolveStrike(
        EntityUid user,
        EntityUid target,
        CFCombatantComponent attackerCombat,
        CFAbilityScoreComponent attackerAbilities,
        CFStrikeWeaponStats weapon,
        float distanceFeet,
        bool propulsive,
        EntityUid weaponUid,
        bool melee,
        int mapPenalty = 0)
    {
        if (!_factions.CanHarm(user, target))
            return;

        if (!_bridge.TryEnsureCombatTarget(target, out var defender, out var hardness, out var isObject))
        {
            _popup.PopupEntity(Loc.GetString("cf-combat-strike-no-combatant"), user, user);
            return;
        }

        // Objects at 0 HP are already destroyed; creatures at 0 HP are dying / unconscious and can be finished.
        var alreadyDown = !isObject && _dying.IsDown(defender);
        if (isObject && defender.CurrentHp <= 0)
            return;

        if (!isObject && TryComp(target, out MobStateComponent? mobState) && _mobState.IsDead(target, mobState))
            return;

        var weaponTrained = IsWeaponTrained(attackerCombat, weapon.Proficiency);
        var attackBonus = mapPenalty + CFMelee.AttackBonus(
            attackerAbilities,
            attackerCombat,
            weapon,
            weaponTrained,
            distanceFeet);

        // Beyond sixth range increment: automatic miss.
        if (weapon.Ranged && attackBonus <= -900)
        {
            RaiseNetworkEvent(
                new CFCombatNumberEvent(GetNetEntity(target), CFStrikeDegree.Failure, 0, weapon.DamageType),
                Filter.Pvs(target));
            return;
        }

        // Downed creatures already take a larger AC penalty. Off-guard does not stack with itself.
        var acPenalty = 0;
        if (alreadyDown)
            acPenalty = DownedAcPenalty;
        else if (melee && !isObject && _flanking.IsOffGuardTo(target, user))
            acPenalty = CFFlanking.OffGuardAcPenalty;

        var targetAc = defender.ArmorClass - acPenalty;
        if (TryComp(target, out CFSpellStatusComponent? ward))
            targetAc += ward.AcBonus;

        var damageMod = CFMelee.DamageBonus(attackerAbilities, weapon, propulsive);
        var d20 = _random.Next(1, 21);
        if (TryComp(user, out CFSpellStatusComponent? boon))
        {
            attackBonus += boon.AttackBonus;
            if (boon.TrueStrike)
            {
                d20 = Math.Max(d20, _random.Next(1, 21));
                boon.TrueStrike = false;
            }
        }

        var degree = CFMelee.DegreeOfSuccess(d20, attackBonus, targetAc);
        var damage = 0;
        var criticalHit = degree == CFStrikeDegree.CriticalSuccess;

        if (degree is CFStrikeDegree.Success or CFStrikeDegree.CriticalSuccess)
        {
            if (alreadyDown)
            {
                // Damage while dying / unconscious at 0 HP increases dying (can kill at dying 4).
                _dying.ApplyDamageWhileDown(target, defender, criticalHit, user);
            }
            else
            {
                damage = CFMelee.RollStrikeDamage(
                    _random,
                    weapon,
                    damageMod,
                    criticalHit);

                if (isObject && hardness > 0)
                    damage = Math.Max(0, damage - hardness);

                if (damage > 0)
                {
                    defender.CurrentHp = Math.Max(0, defender.CurrentHp - damage);
                    Dirty(target, defender);
                    MirrorToSs14(user, target, defender, damage, weapon.DamageType, isObject);
                }

                if (!isObject && defender.CurrentHp <= 0)
                {
                    _popup.PopupEntity(Loc.GetString("cf-combat-strike-defeated"), target, user);
                    _dying.KnockOut(target, defender, criticalHit, user);
                }
                else if (isObject && defender.CurrentHp <= 0)
                {
                    _popup.PopupEntity(Loc.GetString("cf-combat-strike-defeated"), target, user);
                    DestroyObject(user, target);
                }
            }
        }

        PlayStrikeImpact(weaponUid, target, degree, weapon.DamageType, melee);

        RaiseNetworkEvent(
            new CFCombatNumberEvent(GetNetEntity(target), degree, damage, weapon.DamageType),
            Filter.Pvs(target));
    }

    /// <summary>
    /// A miss plays the weapon swing at the target, so it does not use the hit noise.
    /// A hit plays <c>soundHit</c>, or a slash/smash default when the weapon has none.
    /// </summary>
    private void PlayStrikeImpact(EntityUid weaponUid, EntityUid target, CFStrikeDegree degree, string cfDamageType, bool melee)
    {
        if (!melee || !TryComp(weaponUid, out MeleeWeaponComponent? weapon))
            return;

        var sound = degree is CFStrikeDegree.Failure or CFStrikeDegree.CriticalFailure
            ? weapon.SwingSound
            : weapon.HitSound ?? CFMeleeAudio.HitSound(cfDamageType);

        _audio.PlayPvs(sound, Transform(target).Coordinates, sound.Params.WithVariation(0.05f));
    }

    private float GetDistanceFeet(EntityUid from, EntityUid to)
    {
        var a = _transform.GetWorldPosition(from);
        var b = _transform.GetWorldPosition(to);
        var tiles = Vector2.Distance(a, b);
        return CFMelee.TilesToFeet(tiles);
    }

    private bool IsWeaponTrained(CFCombatantComponent attackerCombat, string proficiency)
    {
        if (attackerCombat.ClassId != null
            && _prototypes.TryIndex(attackerCombat.ClassId, out CFClassPrototype? cls))
        {
            return CFAttributeMath.IsWeaponTrained(cls, proficiency);
        }

        return true;
    }

    private void MirrorToSs14(
        EntityUid user,
        EntityUid target,
        CFCombatantComponent defender,
        int cfDamage,
        string damageType,
        bool isObject)
    {
        if (!isObject)
        {
            // CF HP is the source of truth. The SS14 bar is rewritten from it, under the Critical threshold.
            _dying.SyncHealthBar(target, defender);
            return;
        }

        var ss14Type = ToSs14DamageType(damageType);
        var amount = (FixedPoint2) cfDamage;

        if (amount <= 0)
            return;

        var spec = new DamageSpecifier();
        spec.DamageDict[ss14Type] = amount;
        _damageable.TryChangeDamage(target, spec, ignoreResistances: true, origin: user);
    }

    private void DestroyObject(EntityUid user, EntityUid target)
    {
        var finish = new DamageSpecifier();
        finish.DamageDict["Blunt"] = 10000;
        _damageable.TryChangeDamage(target, finish, ignoreResistances: true, origin: user);

        if (!Deleted(target) && !EntityManager.IsQueuedForDeletion(target))
        {
            _destructible.BreakEntity(target);
            if (!Deleted(target) && !EntityManager.IsQueuedForDeletion(target))
                _destructible.DestroyEntity(target);
        }
    }

    private static string ToSs14DamageType(string pf2eType)
    {
        return pf2eType.ToLowerInvariant() switch
        {
            "slashing" or "slash" => "Slash",
            "piercing" => "Piercing",
            "bludgeoning" or "blunt" => "Blunt",
            _ => "Blunt",
        };
    }
}
