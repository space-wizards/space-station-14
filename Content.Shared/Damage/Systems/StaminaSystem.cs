using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.MobState.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Timing;

namespace Content.Shared.Damage;

public sealed class StaminaSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;

    public const string StaminaDamageType = "Stamina";

    /// <summary>
    /// How long after receiving stamina damage before it is allowed to decay.
    /// </summary>
    private const float DecayCooldown = 5f;

    private const float UpdateCooldown = 1f;
    private float _accumulator = 0f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StaminaComponent, DamageChangedEvent>(OnStaminaDamage);
        SubscribeLocalEvent<StaminaComponent, DamageModifyEvent>(OnStaminaModify);
    }

    /// <summary>
    /// Estimate how long it will take to get out of stamina crit.
    /// </summary>
    /// <param name="component"></param>
    /// <returns></returns>
    private TimeSpan GetStamCritTime(StaminaComponent component)
    {
        var damageableSeconds = 0f;

        if (TryComp<MobStateComponent>(component.Owner, out var mobState) &&
            TryComp<DamageableComponent>(component.Owner, out var damageable))
        {
            var crit = mobState.GetEarliestCriticalState(FixedPoint2.Zero);
            damageableSeconds = -MathF.Min(0f, (damageable.Damage.DamageDict[StaminaDamageType].Float() - crit?.threshold.Float() ?? 0f) / component.Decay.DamageDict[StaminaDamageType].Float());
        }

        return TimeSpan.FromSeconds(damageableSeconds);
    }

    private void OnStaminaModify(EntityUid uid, StaminaComponent component, DamageModifyEvent args)
    {
        if (!component.Critical || !args.Damage.DamageDict.ContainsKey(StaminaDamageType)) return;

        // If they're in stamcrit then it will decay and leave it eventually; no perma stun for you.
        args.Damage.DamageDict[StaminaDamageType] = FixedPoint2.Zero;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted) return;

        _accumulator -= frameTime;

        if (_accumulator > 0f) return;

        _accumulator += UpdateCooldown;

        var stamQuery = GetEntityQuery<StaminaComponent>();

        foreach (var active in EntityQuery<ActiveStaminaComponent>())
        {
            // Just in case we have active but not stamina we'll check and account for it.
            if (!stamQuery.TryGetComponent(active.Owner, out var comp))
            {
                RemComp<ActiveStaminaComponent>(active.Owner);
                continue;
            }

            comp.StaminaDecayAccumulator -= UpdateCooldown;

            if (comp.StaminaDecayAccumulator > 0f) continue;

            // We were in crit so come out of it and continue.
            if (comp.Critical)
            {
                ExitStamCrit(active.Owner, comp);
                continue;
            }

            comp.StaminaDecayAccumulator = 0f;
            _damageable.TryChangeDamage(comp.Owner, comp.Decay, true, false);
        }
    }

    private void OnStaminaDamage(EntityUid uid, StaminaComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta == null ||
            !args.DamageDelta.DamageDict.ContainsKey(StaminaDamageType) ||
            !TryComp<MobStateComponent>(uid, out var mobState)) return;

        var crit = mobState.GetEarliestCriticalState(FixedPoint2.Zero);
        var totalStam = FixedPoint2.Zero;

        var belowStamThreshold = crit == null ||
                                 !args.Damageable.Damage.DamageDict.TryGetValue(StaminaDamageType, out totalStam) ||
                                 totalStam < crit.Value.threshold;

        component.StaminaDecayAccumulator = MathF.Max(component.StaminaDecayAccumulator, DecayCooldown);

        // If damage increased check if we need to enter stamcrit.
        if (args.DamageIncreased)
        {
            // We apply stam crit if the amount of stam damage they have exceeds their critical threshold.
            if (belowStamThreshold) return;

            EnterStamCrit(uid, component);
        }
        else if (component.Critical && belowStamThreshold)
        {
            ExitStamCrit(uid, component);
            RemComp<ActiveStaminaComponent>(uid);
            return;
        }

        if (totalStam > FixedPoint2.Zero)
        {
            EnsureComp<ActiveStaminaComponent>(uid);
        }
        else
        {
            RemComp<ActiveStaminaComponent>(uid);
        }
    }

    private void EnterStamCrit(EntityUid uid, StaminaComponent? component = null)
    {
        if (!Resolve(uid, ref component) ||
            component.Critical) return;

        component.Critical = true;

        // Set their stamina to their damage cap (if they have one).
        if (TryComp<DamageableComponent>(uid, out var damageable) &&
            TryComp<MobStateComponent>(uid, out var mobState))
        {
            var excess = component.CritExcess;
            var crit = mobState.GetEarliestCriticalState(FixedPoint2.Zero);
            var stamDamageCap = (crit?.threshold ?? FixedPoint2.Zero) + excess;

            var currentDamage = damageable.Damage;

            if (crit != null)
            {
                currentDamage.DamageDict[StaminaDamageType] = stamDamageCap;
                _damageable.SetDamage(damageable, currentDamage);
            }
        }

        component.StaminaDecayAccumulator = 0f;
        var stunTime = GetStamCritTime(component);
        _stunSystem.TryParalyze(uid, stunTime, true);
        component.StaminaDecayAccumulator = (float) stunTime.TotalSeconds;
    }

    private void ExitStamCrit(EntityUid uid, StaminaComponent? component = null)
    {
        if (!Resolve(uid, ref component) ||
            !component.Critical) return;

        component.Critical = false;

        if (TryComp<DamageableComponent>(uid, out var damageable))
        {
            var existingDamage = damageable.Damage;
            existingDamage.DamageDict[StaminaDamageType] = FixedPoint2.Zero;

            // if they just enter stam crit add a buffer so they can't immediately leave it.
            _damageable.SetDamage(damageable, existingDamage);
        }

        RemComp<ActiveStaminaComponent>(uid);
    }
}
