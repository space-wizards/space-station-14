using System.Diagnostics.CodeAnalysis;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Nutrition.EntitySystems;

public abstract class SatiationSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;

    /// <summary>
    /// A dictionary relating hunger thresholds to corresponding alerts.
    /// </summary>
    protected Dictionary<SatiationThreashold, AlertType>? AlertThresholds;
    protected AlertCategory AlertCategory;
    protected (string, StatusIconPrototype?)[]? Icons;

    public override void Initialize()
    {
        base.Initialize();

        foreach (var pair in Icons!)
        {
            var (iconId, prototype) = pair;
            DebugTools.Assert(_prototype.TryIndex(iconId, out prototype));
        }
    }

    protected void OnMapInit(EntityUid uid, Satiation component, MapInitEvent args)
    {
        var amount = _random.Next(
            (int) component.Thresholds[SatiationThreashold.Concerned] + 10,
            (int) component.Thresholds[SatiationThreashold.Okay]);
        SetNutrition(uid, amount, component);

        component.CurrentThreshold = GetNutritionThreshold(component, component.Current);
        component.LastThreshold = SatiationThreashold.Okay; // TODO: Potentially change this -> Used Okay because no effects.
        // TODO: Check all thresholds make sense and throw if they don't.
        // UpdateEffects(uid, component);

        TryComp(uid, out MovementSpeedModifierComponent? moveMod);
            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid, moveMod);
    }

    protected void OnShutdown(EntityUid uid, Satiation component, ComponentShutdown args)
    {
        _alerts.ClearAlertCategory(uid, AlertCategory);
    }

    protected void OnRefreshMovespeed(EntityUid uid, Satiation component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.CurrentThreshold > SatiationThreashold.Desperate)
            return;

        if (_jetpack.IsUserFlying(uid))
            return;

        args.ModifySpeed(component.SlowdownModifier, component.SlowdownModifier);
    }

    protected void OnRejuvenate(EntityUid uid, Satiation component, RejuvenateEvent args)
    {
        SetNutrition(uid, component.Thresholds[SatiationThreashold.Okay], component);
    }

    /// <summary>
    /// Adds to the current hunger of an entity by the specified value
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="amount"></param>
    /// <param name="component"></param>
    protected void ModifyNutrition(EntityUid uid, float amount, Satiation component)
    {
        SetNutrition(uid, component.Current + amount, component);
    }

    /// <summary>
    /// Sets the current hunger of an entity to the specified value
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="amount"></param>
    /// <param name="component"></param>
    /// public
    protected void SetNutrition(EntityUid uid, float amount, Satiation component)
    {
        component.Current = Math.Clamp(amount,
            component.Thresholds[SatiationThreashold.Dead],
            component.Thresholds[SatiationThreashold.Full]);
        UpdateCurrentThreshold(uid, component);
    }

    protected void UpdateCurrentThreshold(EntityUid uid, Satiation component)
    {
        var calculatedNutritionThreshold = GetNutritionThreshold(component);
        if (calculatedNutritionThreshold == component.CurrentThreshold)
            return;
        component.CurrentThreshold = calculatedNutritionThreshold;
        if (component.ThresholdDamage.TryGetValue(component.CurrentThreshold, out var damage))
            component.CurrentThresholdDamage = damage;
        else
            component.CurrentThresholdDamage = null;
        DoNutritionThresholdEffects(uid, component);
    }

    protected void DoNutritionThresholdEffects(EntityUid uid, Satiation component, bool force = false)
    {
        if (component.CurrentThreshold == component.LastThreshold && !force)
            return;

        if (GetMovementThreshold(component.CurrentThreshold) != GetMovementThreshold(component.LastThreshold))
        {
            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
        }

        if (AlertThresholds!.TryGetValue(component.CurrentThreshold, out var alertId))
        {
            _alerts.ShowAlert(uid, alertId);
        }
        else
        {
            _alerts.ClearAlertCategory(uid, AlertCategory);
        }

        if (component.ThresholdDecayModifiers.TryGetValue(component.CurrentThreshold, out var modifier))
        {
            component.ActualDecayRate = component.BaseDecayRate * modifier;
        }

        component.LastThreshold = component.CurrentThreshold;
    }

    protected void DoContinuousNutritionEffects(EntityUid uid, Satiation component)
    {
        if (!_mobState.IsDead(uid) &&
            component.CurrentThresholdDamage is { } damage)
        {
            _damageable.TryChangeDamage(uid, damage, true, false);
        }
    }

    /// <summary>
    /// Gets the hunger threshold for an entity based on the amount of food specified.
    /// If a specific amount isn't specified, just uses the current hunger of the entity
    /// </summary>
    /// <param name="component"></param>
    /// <param name="nutrition"></param>
    /// <returns></returns>
    // public
    protected SatiationThreashold GetNutritionThreshold(Satiation component, float? nutrition = null)
    {
        nutrition ??= component.Current;
        var result = SatiationThreashold.Dead;
        var value = component.Thresholds[SatiationThreashold.Full];
        foreach (var threshold in component.Thresholds)
        {
            if (threshold.Value <= value && threshold.Value >= nutrition)
            {
                result = threshold.Key;
                value = threshold.Value;
            }
        }
        return result;
    }

    /// <summary>
    /// A check that returns if the entity is below a hunger threshold.
    /// </summary>
    // publci
    protected bool IsNutritionBelowState(EntityUid uid, Satiation component, SatiationThreashold threshold, float? food = null)
    {
        return GetNutritionThreshold(component, food) < threshold;
    }

    private bool GetMovementThreshold(SatiationThreashold threshold)
    {
        switch (threshold)
        {
            case SatiationThreashold.Full:
            case SatiationThreashold.Okay:
                return true;
            case SatiationThreashold.Concerned:
            case SatiationThreashold.Desperate:
            case SatiationThreashold.Dead:
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(threshold), threshold, null);
        }
    }
}

