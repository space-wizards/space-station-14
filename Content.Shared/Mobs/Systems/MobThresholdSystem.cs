using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Mobs.Systems;

public sealed class MobThresholdSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MobThresholdsComponent, MapInitEvent>(MobThresholdMapInit);
        SubscribeLocalEvent<MobThresholdsComponent, ComponentShutdown>(MobThresholdShutdown);
        SubscribeLocalEvent<MobThresholdsComponent, DamageChangedEvent>(OnDamaged);
        SubscribeLocalEvent<MobThresholdsComponent, ComponentGetState>(OnGetComponentState);
        SubscribeLocalEvent<MobThresholdsComponent, ComponentHandleState>(OnHandleComponentState);
    }

    private void OnHandleComponentState(EntityUid uid, MobThresholdsComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MobThresholdComponentState state)
            return;

        component.Thresholds = new SortedDictionary<FixedPoint2, MobState>(state.Thresholds);
        component.CurrentThresholdState = state.CurrentThresholdState;
    }

    private void OnGetComponentState(EntityUid uid, MobThresholdsComponent component, ref ComponentGetState args)
    {
        args.State = new MobThresholdComponentState(component.CurrentThresholdState,
            new Dictionary<FixedPoint2, MobState>(component.Thresholds));
    }

    private void MobThresholdMapInit(EntityUid uid, MobThresholdsComponent component, MapInitEvent args)
    {
        // TODO remove when body sim is implemented
        EnsureComp<MobStateComponent>(uid);
        EnsureComp<DamageableComponent>(uid);

        foreach (var (damage, mobState) in component.Thresholds)
        {
            component.ThresholdReverseLookup.Add(mobState, damage);
        }

        if (!component.Thresholds.TryFirstOrNull(out var newState))
            return;

        TriggerThreshold(uid, MobState.Invalid, newState.Value.Value, thresholds: component);
        UpdateAlerts(uid, newState.Value.Value, component);
    }

    private void MobThresholdShutdown(EntityUid uid, MobThresholdsComponent component, ComponentShutdown args)
    {
        if (component.TriggersAlerts)
            _alerts.ClearAlertCategory(uid, AlertCategory.Health);
    }

    public FixedPoint2 GetThresholdForState(EntityUid uid, MobState mobState,
        MobThresholdsComponent? thresholdComponent)
    {
        if (!Resolve(uid, ref thresholdComponent) ||
            !thresholdComponent.ThresholdReverseLookup.TryGetValue(mobState, out var threshold))
        {
            return FixedPoint2.Zero;
        }

        return threshold;
    }

    public bool TryGetThresholdForState(EntityUid uid, MobState mobState,
        [NotNullWhen(true)] out FixedPoint2? threshold,
        MobThresholdsComponent? thresholdComponent = null)
    {
        if (!Resolve(uid, ref thresholdComponent) ||
            !thresholdComponent.ThresholdReverseLookup.TryGetValue(mobState, out var foundThreshold))
        {
            threshold = null;
            return false;
        }

        threshold = foundThreshold;
        return true;
    }

    public bool TryGetPercentageForState(EntityUid uid, MobState mobState, FixedPoint2 damage,
        [NotNullWhen(true)] out FixedPoint2? percentage,
        MobThresholdsComponent? thresholdComponent = null)
    {
        percentage = null;
        if (!TryGetThresholdForState(uid, mobState, out var threshold, thresholdComponent))
            return false;

        percentage = damage / threshold;
        return true;
    }

    public bool TryGetIncapThreshold(EntityUid uid, [NotNullWhen(true)] out FixedPoint2? threshold,
        MobThresholdsComponent? thresholdComponent = null)
    {
        threshold = null;
        if (!Resolve(uid, ref thresholdComponent))
            return false;

        return TryGetThresholdForState(uid, MobState.Critical, out threshold, thresholdComponent)
               || TryGetThresholdForState(uid, MobState.Dead, out threshold, thresholdComponent);
    }

    public bool TryGetIncapPercentage(EntityUid uid, FixedPoint2 damage,
        [NotNullWhen(true)] out FixedPoint2? percentage,
        MobThresholdsComponent? thresholdComponent = null)
    {
        percentage = null;
        if (!TryGetIncapThreshold(uid, out var threshold, thresholdComponent))
            return false;

        if (damage == 0)
        {
            percentage = 0;
            return true;
        }

        percentage = FixedPoint2.Min(1.0f, damage / threshold.Value);
        return true;
    }

    public bool TryGetDeadThreshold(EntityUid uid, [NotNullWhen(true)] out FixedPoint2? threshold,
        MobThresholdsComponent? thresholdComponent = null)
    {
        threshold = null;
        if (!Resolve(uid, ref thresholdComponent))
            return false;

        return TryGetThresholdForState(uid, MobState.Dead, out threshold, thresholdComponent);
    }

    public bool TryGetDeadPercentage(EntityUid uid, FixedPoint2 damage, [NotNullWhen(true)] out FixedPoint2? percentage,
        MobThresholdsComponent? thresholdComponent = null)
    {
        percentage = null;
        if (!TryGetDeadThreshold(uid, out var threshold, thresholdComponent))
            return false;

        if (damage == 0)
        {
            percentage = 0;
            return true;
        }

        percentage = FixedPoint2.Min(1.0f, damage / threshold.Value);
        return true;
    }


    /// <summary>
    /// Takes the damage from one entity and scales it relative to the health of another
    /// </summary>
    /// <param name="ent1">The entity whose damage will be scaled</param>
    /// <param name="ent2">The entity whose health the damage will scale to</param>
    /// <param name="damage">The newly scaled damage. Can be null</param>
    public bool GetScaledDamage(EntityUid ent1, EntityUid ent2, out DamageSpecifier? damage)
    {
        damage = null;

        if (!TryComp<DamageableComponent>(ent1, out var oldDamage))
            return false;

        if (!TryComp<MobThresholdsComponent>(ent1, out var threshold1) ||
            !TryComp<MobThresholdsComponent>(ent2, out var threshold2))
            return false;

        if (!TryGetThresholdForState(ent1, MobState.Dead, out var ent1DeadThreshold, threshold1))
            ent1DeadThreshold = 0;

        if (!TryGetThresholdForState(ent2, MobState.Dead, out var ent2DeadThreshold, threshold2))
            ent2DeadThreshold = 0;

        damage = (oldDamage.Damage / ent1DeadThreshold.Value) * ent2DeadThreshold.Value;
        return true;
    }

    public void SetMobStateThreshold(EntityUid uid, FixedPoint2 damage, MobState mobState,
        MobThresholdsComponent? thresholdComponent = null)
    {
        if (!Resolve(uid, ref thresholdComponent))
            return;

        if (thresholdComponent.ThresholdReverseLookup.TryGetValue(mobState, out var oldDamage))
            thresholdComponent.Thresholds.Remove(oldDamage);

        thresholdComponent.Thresholds[damage] = mobState;
        thresholdComponent.ThresholdReverseLookup[mobState] = damage;
        VerifyThresholds(uid, thresholdComponent);
    }

    private void OnDamaged(EntityUid uid, MobThresholdsComponent mobThresholdsComponent, DamageChangedEvent args)
    {
        var mobStateComp = EnsureComp<MobStateComponent>(uid);
        CheckThresholds(uid, mobStateComp, mobThresholdsComponent, args.Damageable);
    }

    //Call this if you are somehow change the amount of damage on damageable without triggering a damageChangedEvent
    public void VerifyThresholds(EntityUid target, MobThresholdsComponent? thresholdComponent = null,
        MobStateComponent? mobStateComponent = null, DamageableComponent? damageableComponent = null)
    {
        if (!EnsureComps(target, mobStateComponent, thresholdComponent, damageableComponent))
            return;

        CheckThresholds(target, mobStateComponent, thresholdComponent, damageableComponent);
    }

    private void CheckThresholds(EntityUid target, MobStateComponent mobStateComponent,
        MobThresholdsComponent thresholdsComponent, DamageableComponent damageableComponent)
    {
        foreach (var (threshold, mobState) in thresholdsComponent.Thresholds)
        {
            if (damageableComponent.TotalDamage < threshold)
                continue;

            TriggerThreshold(target, thresholdsComponent.CurrentThresholdState, mobState, mobStateComponent, thresholdsComponent);
        }

        var ev = new MobThresholdDamagedEvent(target, mobStateComponent, thresholdsComponent, damageableComponent);
        RaiseLocalEvent(target, ev, true);
        UpdateAlerts(target, mobStateComponent.CurrentState, thresholdsComponent, damageableComponent);
    }

    private void TriggerThreshold(
        EntityUid uid,
        MobState oldState,
        MobState newState,
        MobStateComponent? mobState = null,
        MobThresholdsComponent? thresholds = null)
    {
        if (oldState == newState ||
            !Resolve(uid, ref mobState, ref thresholds))
        {
            return;
        }

        _mobStateSystem.ReturnMobStateTicket(uid, oldState, mobState);
        thresholds.CurrentThresholdState = newState;
        _mobStateSystem.TakeMobStateTicket(uid, newState, mobState);
        Dirty(uid);
    }

    private bool EnsureComps(EntityUid target, [NotNullWhen(true)] MobStateComponent? mobStateComponent,
        [NotNullWhen(true)] MobThresholdsComponent? thresholdComponent,
        [NotNullWhen(true)] DamageableComponent? damageableComponent)
    {
        if (Resolve(target, ref thresholdComponent) && Resolve(target, ref mobStateComponent) &&
            Resolve(target, ref damageableComponent))
            return true;
        Logger.Error("Target entity does not have damageable or mobThreshold components!");
        return false;
    }

    private void UpdateAlerts(EntityUid uid, MobState currentMobState, MobThresholdsComponent? threshold = null, DamageableComponent? damageable = null)
    {
        if (!Resolve(uid, ref threshold, ref damageable))
            return;

        // don't handle alerts if they are managed by another system... BobbySim (soon TM)
        if (!threshold.TriggersAlerts)
            return;

        switch (currentMobState)
        {
            case MobState.Alive:
            {
                var severity = _alerts.GetMinSeverity(AlertType.HumanHealth);
                if (TryGetIncapPercentage(uid, damageable.TotalDamage, out var percentage))
                {
                    severity = (short)MathF.Floor(percentage.Value.Float() *
                                                  _alerts.GetMaxSeverity(AlertType.HumanHealth));
                }

                _alerts.ShowAlert(uid, AlertType.HumanHealth, severity);
                break;
            }
            case MobState.Critical:
            {
                _alerts.ShowAlert(uid, AlertType.HumanCrit);
                break;
            }
            case MobState.Dead:
            {
                _alerts.ShowAlert(uid, AlertType.HumanDead);
                break;
            }
            case MobState.Invalid:
            default:
                throw new ArgumentOutOfRangeException(nameof(currentMobState), currentMobState, null);
        }
    }
}

public sealed class MobThresholdDamagedEvent : EntityEventArgs
{
    public MobThresholdDamagedEvent(EntityUid Entity, MobStateComponent MobStateComp,
        MobThresholdsComponent ThresholdComp, DamageableComponent DamageableComp)
    {
        this.Entity = Entity;
        this.ThresholdComp = ThresholdComp;
        this.DamageableComp = DamageableComp;
        this.MobStateComp = MobStateComp;
    }

    public EntityUid Entity { get; init; }
    public MobStateComponent MobStateComp { get; init; }
    public MobThresholdsComponent ThresholdComp { get; init; }
    public DamageableComponent DamageableComp { get; init; }
}
