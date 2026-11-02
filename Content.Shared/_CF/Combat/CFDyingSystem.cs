using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.CF.Character;
using Content.Shared.CF.Preferences;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.CF.Combat;

/// <summary>
/// Pathfinder 2e dying / wounded / recovery checks, mapped onto SS14 Critical / Dead.
/// </summary>
public sealed partial class CFDyingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _mobThresholds = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;

    public override void Update(float frameTime)
    {
        // Game tick, not a physics substep. Only creatures that still have the dying condition are checked,
        // and each one at most once per recovery interval.
        if (!_net.IsServer)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<CFDyingTrackerComponent, CFCombatantComponent>();
        while (query.MoveNext(out var uid, out var tracker, out var combatant))
        {
            if (combatant.Dying <= 0)
            {
                RemCompDeferred<CFDyingTrackerComponent>(uid);
                continue;
            }

            if (now < tracker.NextRecoveryCheck)
                continue;

            tracker.NextRecoveryCheck = now + TimeSpan.FromSeconds(CFDyingTrackerComponent.RecoveryIntervalSeconds);
            Dirty(uid, tracker);
            AttemptRecoveryCheck(uid, combatant);
        }
    }

    /// <summary>
    /// Stock revive heals SS14 damage and sets the mob Alive, but leaves Pathfinder HP at 0.
    /// That kept the creature "down", so flanking never applied again.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnRejuvenate(Entity<CFCombatantComponent> ent, ref RejuvenateEvent args)
    {
        if (!_net.IsServer)
            return;

        ent.Comp.Dying = 0;
        ent.Comp.CurrentHp = Math.Max(1, ent.Comp.MaxHp);
        Dirty(ent.Owner, ent.Comp);
        RemComp<CFDyingTrackerComponent>(ent.Owner);

        var revived = new CFCombatantRevivedEvent();
        RaiseLocalEvent(ent.Owner, ref revived);
    }

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<CFCombatantComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        // Stabilize is only for the dying condition — never for the fully dead.
        if (!CanStabilize(ent.Owner, ent.Comp))
            return;

        if (!TryComp(args.User, out CFAbilityScoreComponent? healerAbilities)
            || !TryComp(args.User, out CFCombatantComponent? healerCombat))
            return;

        var user = args.User;
        var target = ent.Owner;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("cf-combat-stabilize-verb"),
            Act = () => AttemptStabilize(user, target, healerAbilities, healerCombat),
            Priority = 2,
        });
    }

    /// <summary>
    /// True while the creature has the PF2e dying condition (0 HP, not yet dead).
    /// Dead creatures need Raise Dead / Resurrect — Stabilize does nothing.
    /// </summary>
    public bool CanStabilize(EntityUid target, CFCombatantComponent? combatant = null)
    {
        if (!Resolve(target, ref combatant, logMissing: false))
            return false;

        if (combatant.Dying <= 0 || combatant.CurrentHp > 0)
            return false;

        if (_mobState.IsDead(target))
            return false;

        return true;
    }

    /// <summary>
    /// First time reduced to 0 HP: unconscious + dying (1, or 2 on critical), plus wounded.
    /// </summary>
    public void KnockOut(
        EntityUid target,
        CFCombatantComponent combatant,
        bool fromCriticalHit,
        EntityUid? origin = null)
    {
        combatant.CurrentHp = 0;
        var dying = fromCriticalHit ? 2 : 1;
        dying += combatant.Wounded;
        SetDying(target, combatant, dying, origin, knockedOut: true);
        SyncSs14Critical(target, origin);
        PopupPvs(Loc.GetString("cf-combat-dying-gain", ("value", combatant.Dying)), target, PopupType.MediumCaution);
    }

    /// <summary>
    /// Damage while already at 0 HP: increase dying (crit +2), or gain dying 1 if stabilized.
    /// </summary>
    public void ApplyDamageWhileDown(
        EntityUid target,
        CFCombatantComponent combatant,
        bool criticalHit,
        EntityUid? origin = null)
    {
        if (combatant.CurrentHp > 0)
            return;

        if (combatant.Dying <= 0)
        {
            // Stabilized / unconscious at 0 HP — taking damage puts you back into dying.
            var dying = 1 + combatant.Wounded;
            SetDying(target, combatant, dying, origin, knockedOut: false);
            SyncSs14Critical(target, origin);
            PopupPvs(Loc.GetString("cf-combat-dying-gain", ("value", combatant.Dying)), target, PopupType.MediumCaution);
            return;
        }

        var increase = criticalHit ? 2 : 1;
        SetDying(target, combatant, combatant.Dying + increase, origin, knockedOut: false);
        PopupPvs(Loc.GetString("cf-combat-dying-increase", ("value", combatant.Dying)), target, PopupType.MediumCaution);
    }

    /// <summary>
    /// Restore CF hit points and heal the same amount across all SS14 damage types evenly.
    /// At 1+ CF HP, clears dying and ensures the mob is Alive (pulls under Critical if needed).
    /// Does nothing to fully dead creatures (use Raise Dead / Resurrect for that).
    /// </summary>
    public void RestoreHitPoints(EntityUid target, int amount, EntityUid? healer = null)
    {
        if (amount <= 0)
            return;

        // Healing / potions cannot revive the dead.
        if (_mobState.IsDead(target))
            return;

        var healedCf = 0;
        if (TryComp(target, out CFCombatantComponent? combatant))
        {
            var wasDown = combatant.CurrentHp <= 0;
            var before = combatant.CurrentHp;
            combatant.CurrentHp = Math.Min(combatant.MaxHp, combatant.CurrentHp + amount);
            healedCf = combatant.CurrentHp - before;
            Dirty(target, combatant);

            if (wasDown && combatant.CurrentHp >= 1)
            {
                LoseDying(target, combatant, gainWounded: true);
                PopupPvs(Loc.GetString("cf-combat-dying-healed"), target, PopupType.Medium);
            }
        }

        // Distribute the rolled heal across Blunt/Slash/Piercing/Poison/etc.
        if (TryComp(target, out DamageableComponent? damageable))
            _damageable.HealEvenly((target, damageable), -amount, origin: healer);

        if (combatant != null && combatant.CurrentHp >= 1)
            EnsureConsciousSs14(target, healer);

        _ = healedCf;
    }

    /// <summary>
    /// Medicine Stabilize — on success, clears dying, sets CF HP to 1, wipes SS14 damage, and wakes Alive.
    /// (SS14 crit thresholds don't model "unconscious at 0 HP", so we revive to 1 HP instead.)
    /// </summary>
    public void AttemptStabilize(
        EntityUid healer,
        EntityUid target,
        CFAbilityScoreComponent healerAbilities,
        CFCombatantComponent healerCombat)
    {
        if (!_net.IsServer)
            return;

        if (!TryComp(target, out CFCombatantComponent? patient) || !CanStabilize(target, patient))
        {
            if (_mobState.IsDead(target))
            {
                _popup.PopupEntity(
                    Loc.GetString("cf-combat-stabilize-dead"),
                    healer,
                    healer,
                    PopupType.SmallCaution);
            }

            return;
        }

        CFClassPrototype? cls = null;
        if (healerCombat.ClassId != null)
            _prototypes.TryIndex(healerCombat.ClassId, out cls);

        CFBackgroundPrototype? background = null;
        if (healerCombat.BackgroundId != null)
            _prototypes.TryIndex(healerCombat.BackgroundId, out background);

        var rank = CFSkills.GetSkillRank(cls, background, "Medicine", healerCombat.TrainedSkills);
        var bonus = CFSkills.SkillModifier(healerAbilities.AbilityModifiers, CFAbility.Wisdom, healerCombat.Level, rank);
        var dc = CFDyingTrackerComponent.BaseStabilizeDc + patient.Dying;
        var d20 = _random.Next(1, 21);
        var degree = CFMelee.DegreeOfSuccess(d20, bonus, dc);
        var total = d20 + bonus;

        _popup.PopupEntity(
            Loc.GetString("cf-combat-stabilize-roll",
                ("d20", d20),
                ("mod", CFSkills.FormatSigned(bonus)),
                ("total", total),
                ("dc", dc)),
            healer,
            healer);

        if (degree is CFStrikeDegree.Success or CFStrikeDegree.CriticalSuccess)
        {
            ReviveAtOneHitPoint(target, patient, healer);
            PopupPvs(Loc.GetString("cf-combat-stabilize-success"), target, PopupType.Medium);
        }
        else
        {
            _popup.PopupEntity(
                Loc.GetString("cf-combat-stabilize-failure"),
                healer,
                healer,
                PopupType.SmallCaution);
        }
    }

    /// <summary>
    /// Clear dying, set CF HP to 1, wipe SS14 damage/bleed, and force Alive.
    /// Refuses fully dead targets.
    /// </summary>
    public void ReviveAtOneHitPoint(EntityUid target, CFCombatantComponent combatant, EntityUid? healer = null)
    {
        if (_mobState.IsDead(target))
            return;

        LoseDying(target, combatant, gainWounded: true);
        combatant.CurrentHp = 1;
        Dirty(target, combatant);

        ClearSs14Trauma(target);
        ForceAlive(target, healer);
    }

    public bool IsDown(CFCombatantComponent combatant) => combatant.CurrentHp <= 0;

    public bool IsDying(CFCombatantComponent combatant) => combatant.Dying > 0;

    private void AttemptRecoveryCheck(EntityUid uid, CFCombatantComponent combatant)
    {
        var dc = CFDyingTrackerComponent.BaseRecoveryDc + combatant.Dying;
        var d20 = _random.Next(1, 21);
        var degree = CFMelee.DegreeOfSuccess(d20, 0, dc);

        var delta = degree switch
        {
            CFStrikeDegree.CriticalSuccess => -2,
            CFStrikeDegree.Success => -1,
            CFStrikeDegree.Failure => 1,
            CFStrikeDegree.CriticalFailure => 2,
            _ => 0,
        };

        PopupPvs(
            Loc.GetString("cf-combat-recovery-roll",
                ("d20", d20),
                ("dc", dc),
                ("result", Loc.GetString($"cf-combat-recovery-{DegreeKey(degree)}"))),
            uid,
            PopupType.Medium);

        SetDying(uid, combatant, combatant.Dying + delta, origin: null, knockedOut: false);

        if (combatant.Dying <= 0)
        {
            // Recovery stabilized — revive at 1 HP so SS14 leaves Critical.
            ReviveAtOneHitPoint(uid, combatant);
            PopupPvs(Loc.GetString("cf-combat-recovery-stabilize"), uid, PopupType.Medium);
        }
    }

    private void SetDying(
        EntityUid target,
        CFCombatantComponent combatant,
        int value,
        EntityUid? origin,
        bool knockedOut)
    {
        combatant.Dying = Math.Max(0, value);
        Dirty(target, combatant);

        if (combatant.Dying >= CFDyingTrackerComponent.DeathValue)
        {
            Die(target, combatant, origin);
            return;
        }

        if (combatant.Dying > 0)
        {
            var tracker = EnsureComp<CFDyingTrackerComponent>(target);
            if (knockedOut || tracker.NextRecoveryCheck == default)
            {
                tracker.NextRecoveryCheck = _timing.CurTime
                    + TimeSpan.FromSeconds(CFDyingTrackerComponent.RecoveryIntervalSeconds);
                Dirty(target, tracker);
            }
        }
        else
        {
            RemCompDeferred<CFDyingTrackerComponent>(target);
        }
    }

    private void LoseDying(EntityUid target, CFCombatantComponent combatant, bool gainWounded)
    {
        // PF2e: anytime you lose dying, gain wounded 1 or increase wounded by 1.
        if (gainWounded)
            combatant.Wounded = combatant.Wounded <= 0 ? 1 : combatant.Wounded + 1;

        combatant.Dying = 0;
        Dirty(target, combatant);
        RemCompDeferred<CFDyingTrackerComponent>(target);
    }

    private void Die(EntityUid target, CFCombatantComponent combatant, EntityUid? origin)
    {
        // Clear dying — the creature is dead, not dying. Stabilize must not apply.
        combatant.Dying = 0;
        combatant.CurrentHp = 0;
        Dirty(target, combatant);
        RemCompDeferred<CFDyingTrackerComponent>(target);

        SyncSs14Dead(target, origin);
        PopupPvs(Loc.GetString("cf-combat-dying-death"), target, PopupType.LargeCaution);
    }

    private void PopupPvs(string message, EntityUid target, PopupType type)
    {
        _popup.PopupEntity(message, target, Filter.Pvs(target), recordReplay: true, type);
    }

    private void WakeAlive(EntityUid target, EntityUid? origin)
    {
        // Full revive (stabilize): wipe SS14 damage/bleed so MobThresholds can't keep them Critical.
        ClearSs14Trauma(target);
        ForceAlive(target, origin);
    }

    /// <summary>
    /// After partial healing, rewrite the SS14 bar from remaining CF HP so MobThresholds cannot leave them Critical.
    /// </summary>
    private void EnsureConsciousSs14(EntityUid target, EntityUid? origin)
    {
        if (TryComp(target, out CFCombatantComponent? combatant))
            SyncSs14FromHp(target, combatant);

        ForceAlive(target, origin);
    }

    /// <summary>
    /// Clears Damageable + stops Bloodstream bleeding (and tops blood back up).
    /// Damage alone does not stop puddle bleeding.
    /// </summary>
    private void ClearSs14Trauma(EntityUid target)
    {
        if (TryComp(target, out DamageableComponent? damageable))
            _damageable.ClearAllDamage((target, damageable));

        if (!TryComp(target, out BloodstreamComponent? blood))
            return;

        if (blood.BleedAmount > 0)
            _bloodstream.TryModifyBleedAmount((target, blood), -blood.BleedAmount);

        // Refill so bloodloss damage doesn't immediately put them back under.
        _bloodstream.TryModifyBloodLevel((target, blood), blood.BloodReferenceSolution.Volume);
    }

    private void ForceAlive(EntityUid target, EntityUid? origin)
    {
        if (TryComp(target, out MobStateComponent? mob) && mob.CurrentState != MobState.Alive)
            _mobState.ChangeMobState(target, MobState.Alive, mob, origin);
    }

    private void SyncSs14Critical(EntityUid target, EntityUid? origin)
    {
        if (!TryComp(target, out MobStateComponent? mob))
            return;

        // Put damage just at the Critical threshold so MobThresholds agrees with Critical.
        if (_mobThresholds.TryGetThresholdForState(target, MobState.Critical, out var crit) && crit != null)
        {
            var amount = crit.Value;
            if (_mobThresholds.TryGetThresholdForState(target, MobState.Dead, out var dead)
                && dead != null
                && dead.Value <= amount)
                amount = FixedPoint2.Max(FixedPoint2.Zero, dead.Value - FixedPoint2.Epsilon);

            SetSs14Damage(target, amount);
        }

        if (mob.CurrentState != MobState.Critical)
            _mobState.ChangeMobState(target, MobState.Critical, mob, origin);
    }

    private void SyncSs14Dead(EntityUid target, EntityUid? origin)
    {
        if (!TryComp(target, out MobStateComponent? mob))
            return;

        if (_mobThresholds.TryGetThresholdForState(target, MobState.Dead, out var dead) && dead != null)
            SetSs14Damage(target, dead.Value);

        if (mob.CurrentState != MobState.Dead)
            _mobState.ChangeMobState(target, MobState.Dead, mob, origin);
    }

    /// <summary>
    /// Rewrite the SS14 bar from remaining CF HP. Conscious creatures stay under the Critical threshold.
    /// </summary>
    public void SyncHealthBar(EntityUid target, CFCombatantComponent combatant)
    {
        if (combatant.CurrentHp <= 0)
            return;

        SyncSs14FromHp(target, combatant);
    }

    private void SyncSs14FromHp(EntityUid target, CFCombatantComponent combatant)
    {
        if (!_mobThresholds.TryGetThresholdForState(target, MobState.Critical, out var crit) || crit == null)
            return;

        // Map missing CF HP onto damage below the crit threshold while conscious.
        FixedPoint2 desired;
        if (combatant.MaxHp <= 0)
            desired = FixedPoint2.Zero;
        else
        {
            var missing = combatant.MaxHp - combatant.CurrentHp;
            desired = crit.Value * missing / combatant.MaxHp;
            if (desired >= crit.Value)
                desired = crit.Value - FixedPoint2.Epsilon;
        }

        SetSs14Damage(target, FixedPoint2.Max(FixedPoint2.Zero, desired));
    }

    /// <summary>
    /// Replace SS14 damage with one Blunt value. The bar is a mirror of CF HP, not a sum of old damage.
    /// </summary>
    private void SetSs14Damage(EntityUid target, FixedPoint2 amount)
    {
        if (!TryComp(target, out DamageableComponent? damageable))
            return;

        var spec = new DamageSpecifier();
        if (amount > FixedPoint2.Zero)
            spec.DamageDict["Blunt"] = amount;

        _damageable.SetDamage((target, damageable), spec);
    }

    private static string DegreeKey(CFStrikeDegree degree) => degree switch
    {
        CFStrikeDegree.CriticalSuccess => "crit-success",
        CFStrikeDegree.Success => "success",
        CFStrikeDegree.Failure => "failure",
        CFStrikeDegree.CriticalFailure => "crit-failure",
        _ => "failure",
    };
}

/// <summary>
/// Raised after a rejuvenate restores this creature's Pathfinder hit points.
/// </summary>
[ByRefEvent]
public readonly record struct CFCombatantRevivedEvent;
