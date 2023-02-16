using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Robust.Shared.GameStates;
namespace Content.Shared.Mobs.Systems;

public sealed class MobThresholdSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MobThresholdsComponent, ComponentShutdown>(MobThresholdShutdown);
        SubscribeLocalEvent<MobThresholdsComponent, ComponentStartup>(MobThresholdStartup);
        SubscribeLocalEvent<MobThresholdsComponent, DamageChangedEvent>(OnDamaged);
        SubscribeLocalEvent<MobThresholdsComponent, ComponentGetState>(OnGetComponentState);
        SubscribeLocalEvent<MobThresholdsComponent, ComponentHandleState>(OnHandleComponentState);
        SubscribeLocalEvent<MobThresholdsComponent, UpdateMobStateEvent>(OnUpdateMobState);
    }

    #region Public API

    /// <summary>
    /// Get the Damage Threshold for the appropriate state if it exists
    /// </summary>
    /// <param name="target">Target Entity</param>
    /// <param name="mobState">MobState we want the Damage Threshold of</param>
    /// <param name="thresholdComponent">Threshold Component Owned by the target</param>
    /// <returns>the threshold or 0 if it doesn't exist</returns>
    public FixedPoint2 GetThresholdForState(EntityUid target, MobState mobState,
        MobThresholdsComponent? thresholdComponent = null)
    {
        if (!Resolve(target, ref thresholdComponent))
            return FixedPoint2.Zero;

        foreach (var pair in thresholdComponent.Thresholds)
        {
            if (pair.Value == mobState)
            {
                return pair.Key;
            }
        }

        return FixedPoint2.Zero;
    }

    /// <summary>
    /// Try to get the Damage Threshold for the appropriate state if it exists
    /// </summary>
    /// <param name="target">Target Entity</param>
    /// <param name="mobState">MobState we want the Damage Threshold of</param>
    /// <param name="threshold">The damage Threshold for the given state</param>
    /// <param name="thresholdComponent">Threshold Component Owned by the target</param>
    /// <returns>true if successfully retrieved a threshold</returns>
    public bool TryGetThresholdForState(EntityUid target, MobState mobState,
        [NotNullWhen(true)] out FixedPoint2? threshold,
        MobThresholdsComponent? thresholdComponent = null)
    {
        threshold = null;
        if (!Resolve(target, ref thresholdComponent))
            return false;

        foreach (var pair in thresholdComponent.Thresholds)
        {
            if (pair.Value == mobState)
            {
                threshold = pair.Key;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Try to get the a percentage of the Damage Threshold for the appropriate state if it exists
    /// </summary>
    /// <param name="target">Target Entity</param>
    /// <param name="mobState">MobState we want the Damage Threshold of</param>
    /// <param name="damage">The Damage being applied</param>
    /// <param name="percentage">Percentage of Damage compared to the Threshold</param>
    /// <param name="thresholdComponent">Threshold Component Owned by the target</param>
    /// <returns>true if successfully retrieved a percentage</returns>
    public bool TryGetPercentageForState(EntityUid target, MobState mobState, FixedPoint2 damage,
        [NotNullWhen(true)] out FixedPoint2? percentage,
        MobThresholdsComponent? thresholdComponent = null)
    {
        percentage = null;
        if (!TryGetThresholdForState(target, mobState, out var threshold, thresholdComponent))
            return false;

        percentage = damage / threshold;
        return true;
    }

    /// <summary>
    /// Try to get the Damage Threshold for crit or death. Outputs the first found threshold.
    /// </summary>
    /// <param name="target">Target Entity</param>
    /// <param name="threshold">The Damage Threshold for incapacitation</param>
    /// <param name="thresholdComponent">Threshold Component owned by the target</param>
    /// <returns>true if successfully retrieved incapacitation threshold</returns>
    public bool TryGetIncapThreshold(EntityUid target, [NotNullWhen(true)] out FixedPoint2? threshold,
        MobThresholdsComponent? thresholdComponent = null)
    {
        threshold = null;
        if (!Resolve(target, ref thresholdComponent))
            return false;

        return TryGetThresholdForState(target, MobState.Critical, out threshold, thresholdComponent)
               || TryGetThresholdForState(target, MobState.Dead, out threshold, thresholdComponent);
    }

    /// <summary>
    /// Try to get a percentage of the Damage Threshold for crit or death. Outputs the first found percentage.
    /// </summary>
    /// <param name="target">Target Entity</param>
    /// <param name="damage">The damage being applied</param>
    /// <param name="percentage">Percentage of Damage compared to the Incapacitation Threshold</param>
    /// <param name="thresholdComponent">Threshold Component Owned by the target</param>
    /// <returns>true if successfully retrieved incapacitation percentage</returns>
    public bool TryGetIncapPercentage(EntityUid target, FixedPoint2 damage,
        [NotNullWhen(true)] out FixedPoint2? percentage,
        MobThresholdsComponent? thresholdComponent = null)
    {
        percentage = null;
        if (!TryGetIncapThreshold(target, out var threshold, thresholdComponent))
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
    /// Try to get the Damage Threshold for death
    /// </summary>
    /// <param name="target">Target Entity</param>
    /// <param name="threshold">The Damage Threshold for death</param>
    /// <param name="thresholdComponent">Threshold Component owned by the target</param>
    /// <returns>true if successfully retrieved incapacitation threshold</returns>
    public bool TryGetDeadThreshold(EntityUid target, [NotNullWhen(true)] out FixedPoint2? threshold,
        MobThresholdsComponent? thresholdComponent = null)
    {
        threshold = null;
        if (!Resolve(target, ref thresholdComponent))
            return false;

        return TryGetThresholdForState(target, MobState.Dead, out threshold, thresholdComponent);
    }

    /// <summary>
    /// Try to get a percentage of the Damage Threshold for death
    /// </summary>
    /// <param name="target">Target Entity</param>
    /// <param name="damage">The damage being applied</param>
    /// <param name="percentage">Percentage of Damage compared to the Death Threshold</param>
    /// <param name="thresholdComponent">Threshold Component Owned by the target</param>
    /// <returns>true if successfully retrieved death percentage</returns>
    public bool TryGetDeadPercentage(EntityUid target, FixedPoint2 damage,
        [NotNullWhen(true)] out FixedPoint2? percentage,
        MobThresholdsComponent? thresholdComponent = null)
    {
        percentage = null;
        if (!TryGetDeadThreshold(target, out var threshold, thresholdComponent))
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
    /// <param name="target1">The entity whose damage will be scaled</param>
    /// <param name="target2">The entity whose health the damage will scale to</param>
    /// <param name="damage">The newly scaled damage. Can be null</param>
    public bool GetScaledDamage(EntityUid target1, EntityUid target2, out DamageSpecifier? damage)
    {
        damage = null;

        if (!TryComp<DamageableComponent>(target1, out var oldDamage))
            return false;

        if (!TryComp<MobThresholdsComponent>(target1, out var threshold1) ||
            !TryComp<MobThresholdsComponent>(target2, out var threshold2))
            return false;

        if (!TryGetThresholdForState(target1, MobState.Dead, out var ent1DeadThreshold, threshold1))
            ent1DeadThreshold = 0;

        if (!TryGetThresholdForState(target2, MobState.Dead, out var ent2DeadThreshold, threshold2))
            ent2DeadThreshold = 0;

        damage = (oldDamage.Damage / ent1DeadThreshold.Value) * ent2DeadThreshold.Value;
        return true;
    }

    /// <summary>
    /// Set a MobState Threshold or create a new one if it doesn't exist
    /// </summary>
    /// <param name="target">Target Entity</param>
    /// <param name="damage">Damageable Component owned by the target</param>
    /// <param name="mobState">MobState Component owned by the target</param>
    /// <param name="threshold">MobThreshold Component owned by the target</param>
    public void SetMobStateThreshold(EntityUid target, FixedPoint2 damage, MobState mobState,
        MobThresholdsComponent? threshold = null)
    {
        if (!Resolve(target, ref threshold))
            return;

        threshold.Thresholds[damage] = mobState;
        VerifyThresholds(target, threshold);
    }

    /// <summary>
    /// Checks to see if we should change states based on thresholds.
    /// Call this if you change the amount of damagable without triggering a damageChangedEvent or if you change
    /// </summary>
    /// <param name="target">Target Entity</param>
    /// <param name="threshold">Threshold Component owned by the Target</param>
    /// <param name="mobState">MobState Component owned by the Target</param>
    /// <param name="damageable">Damageable Component owned by the Target</param>
    public void VerifyThresholds(EntityUid target, MobThresholdsComponent? threshold = null,
        MobStateComponent? mobState = null, DamageableComponent? damageable = null)
    {
        if (!Resolve(target, ref mobState, ref threshold, ref damageable))
            return;

        CheckThresholds(target, mobState, threshold, damageable);

        var ev = new MobThresholdChecked(target, mobState, threshold, damageable);
        RaiseLocalEvent(target, ref ev, true);
        UpdateAlerts(target, mobState.CurrentState, threshold, damageable);
    }

    #endregion

    #region Private Implementation

    private void CheckThresholds(EntityUid target, MobStateComponent mobStateComponent,
        MobThresholdsComponent thresholdsComponent, DamageableComponent damageableComponent)
    {
        foreach (var (threshold, mobState) in thresholdsComponent.Thresholds.Reverse())
        {
            if (damageableComponent.TotalDamage < threshold)
                continue;

            TriggerThreshold(target, mobState, mobStateComponent, thresholdsComponent);
            break;
        }
    }

    private void TriggerThreshold(
        EntityUid target,
        MobState newState,
        MobStateComponent? mobState = null,
        MobThresholdsComponent? thresholds = null)
    {
        if (!Resolve(target, ref mobState, ref thresholds) ||
            mobState.CurrentState == newState)
        {
            return;
        }

        thresholds.CurrentThresholdState = newState;
        _mobStateSystem.UpdateMobState(target, mobState);

        Dirty(target);
    }

    private void UpdateAlerts(EntityUid target, MobState currentMobState, MobThresholdsComponent? threshold = null,
        DamageableComponent? damageable = null)
    {
        if (!Resolve(target, ref threshold, ref damageable))
            return;

        // don't handle alerts if they are managed by another system... BobbySim (soon TM)
        if (!threshold.TriggersAlerts)
            return;

        switch (currentMobState)
        {
            case MobState.Alive:
            {
                var severity = _alerts.GetMinSeverity(AlertType.HumanHealth);
                if (TryGetIncapPercentage(target, damageable.TotalDamage, out var percentage))
                {
                    severity = (short) MathF.Floor(percentage.Value.Float() *
                                                   _alerts.GetSeverityRange(AlertType.HumanHealth));
                    severity += _alerts.GetMinSeverity(AlertType.HumanHealth);
                }
                _alerts.ShowAlert(target, AlertType.HumanHealth, severity);
                break;
            }
            case MobState.Critical:
            {
                _alerts.ShowAlert(target, AlertType.HumanCrit);
                break;
            }
            case MobState.Dead:
            {
                _alerts.ShowAlert(target, AlertType.HumanDead);
                break;
            }
            case MobState.Invalid:
            default:
                throw new ArgumentOutOfRangeException(nameof(currentMobState), currentMobState, null);
        }
    }

    private void OnDamaged(EntityUid target, MobThresholdsComponent thresholds, DamageChangedEvent args)
    {
        if (!TryComp<MobStateComponent>(target, out var mobState))
            return;
        CheckThresholds(target, mobState, thresholds, args.Damageable);
        var ev = new MobThresholdChecked(target, mobState, thresholds, args.Damageable);
        RaiseLocalEvent(target, ref ev, true);
        UpdateAlerts(target, mobState.CurrentState, thresholds, args.Damageable);
    }

    private void OnHandleComponentState(EntityUid target, MobThresholdsComponent component,
        ref ComponentHandleState args)
    {
        if (args.Current is not MobThresholdComponentState state)
            return;

        component.Thresholds = new SortedDictionary<FixedPoint2, MobState>(state.Thresholds);
        component.CurrentThresholdState = state.CurrentThresholdState;
    }

    private void OnGetComponentState(EntityUid target, MobThresholdsComponent component, ref ComponentGetState args)
    {
        args.State = new MobThresholdComponentState(component.CurrentThresholdState,
            new Dictionary<FixedPoint2, MobState>(component.Thresholds));
    }

    private void MobThresholdStartup(EntityUid target, MobThresholdsComponent thresholds, ComponentStartup args)
    {
        if (!TryComp<MobStateComponent>(target, out var mobState) || !TryComp<DamageableComponent>(target, out var damageable))
            return;
        CheckThresholds(target, mobState, thresholds, damageable);
        var ev = new MobThresholdChecked(target, mobState, thresholds, damageable);
        RaiseLocalEvent(target, ref ev, true);
        UpdateAlerts(target, mobState.CurrentState, thresholds, damageable);
    }

    private void MobThresholdShutdown(EntityUid target, MobThresholdsComponent component, ComponentShutdown args)
    {
        if (component.TriggersAlerts)
            _alerts.ClearAlertCategory(target, AlertCategory.Health);
    }

    private void OnUpdateMobState(EntityUid target, MobThresholdsComponent component, ref UpdateMobStateEvent args)
    {
        if (component.CurrentThresholdState != MobState.Invalid)
            args.State = component.CurrentThresholdState;
    }

    #endregion
}

/// <summary>
/// Event that triggers when an entity with a mob threshold is checked
/// </summary>
/// <param name="Target">Target entity</param>
/// <param name="Threshold">Threshold Component owned by the Target</param>
/// <param name="MobState">MobState Component owned by the Target</param>
/// <param name="Damageable">Damageable Component owned by the Target</param>
[ByRefEvent]
public readonly record struct MobThresholdChecked(EntityUid Target, MobStateComponent MobState,
    MobThresholdsComponent Threshold, DamageableComponent Damageable)
{
}
