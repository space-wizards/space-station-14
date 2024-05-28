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

public sealed class SatiationSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SatiationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SatiationComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SatiationComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<SatiationComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnMapInit(EntityUid uid, SatiationComponent component, MapInitEvent args)
    {
        foreach (var (_, satiation) in component.Satiations)
        {
            var amount = _random.Next(
                (int) satiation.Prototype.Thresholds[SatiationThreashold.Concerned] + 10,
                (int) satiation.Prototype.Thresholds[SatiationThreashold.Okay]);
            SetSatiation(satiation, amount);
            UpdateCurrentThreshold(satiation);
            DoThresholdEffects(uid, satiation, satiation.Prototype.alertThresholds, satiation.Prototype.alertCategory, false);

            satiation.CurrentThreshold = GetThreshold(satiation, satiation.Current);
            satiation.LastThreshold = SatiationThreashold.Okay; // TODO: Potentially change this -> Used Okay because no effects.
        }
        Dirty(uid, component);

        if (TryComp(uid, out MovementSpeedModifierComponent? moveMod))
            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid, moveMod);
    }

    private void OnShutdown(EntityUid uid, SatiationComponent component, ComponentShutdown args)
    {
        foreach(var (_, satiation) in component.Satiations)
        {
            _alerts.ClearAlertCategory(uid, satiation.Prototype.AlertCategory);
        }
    }

    private void OnRefreshMovespeed(EntityUid uid, SatiationComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (_jetpack.IsUserFlying(uid))
            return;

        foreach(var (_, satiation) in component.Satiations)
        {
            args.ModifySpeed(satiation.Prototype.SlowdownModifier, satiation.Prototype.SlowdownModifier);
        }
    }

    private void OnRejuvenate(EntityUid uid, SatiationComponent component, RejuvenateEvent args)
    {
        foreach(var (_, satiation) in component.Satiations)
        {
            args.ModifySpeed(satiation.Prototype.SlowdownModifier, satiation.Prototype.SlowdownModifier);
            SetSatiation((uid, component), satiation.Prototype.Thresholds[SatiationThreashold.Okay]);
        }
    }

    public void ModifySatiation(Satiation satiation, float amount)
    {
        SetSatiation(satiation, satiation.Current + amount);
    }

    private void SetSatiation(Satiation satiation, float amount)
    {
        satiation.Current = Math.Clamp(amount,
            satiation.Prototype.Thresholds[SatiationThreashold.Dead],
            satiation.Prototype.Thresholds[SatiationThreashold.Full]);
    }

    private void UpdateCurrentThreshold(EntityUid uid, Satiation satiation)
    {
            var calculatedNutritionThreshold = GetThreshold(satiation);
            if (calculatedNutritionThreshold == satiation.CurrentThreshold)
                return;
            satiation.CurrentThreshold = calculatedNutritionThreshold;
            if (satiation.Prototype.ThresholdDamage.TryGetValue(satiation.CurrentThreshold, out var damage))
                satiation.CurrentThresholdDamage = damage;
            else
                satiation.CurrentThresholdDamage = null;
            DoThresholdEffects(uid, satiation);
    }

    private bool DoThresholdEffects(EntityUid uid, Satiation satiation, bool force = false)
    {
        if (satiation.CurrentThreshold == satiation.LastThreshold && !force)
            return false;

        if (GetMovementThreshold(satiation.CurrentThreshold) != GetMovementThreshold(satiation.LastThreshold))
        {
            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
        }
        if (satiation.Prototype.ThresholdDecayModifiers.TryGetValue(satiation.CurrentThreshold, out var modifier))
        {
            satiation.ActualDecayRate = satiation.BaseDecayRate * modifier;
        }
        satiation.LastThreshold = satiation.CurrentThreshold;

        if (satiation.Prototype.AlertCategory.TryGetValue(satiation.CurrentThreshold, out var alertId))
        {
            _alerts.ShowAlert(uid, alertId);
        }
        else
        {
            _alerts.ClearAlertCategory(uid, satiation.Prototype.AlertCategory);
        }

        return true;
    }

    private void DoContinuousEffects(EntityUid uid, Satiation satiation)
    {
        if (!_mobState.IsDead(uid) &&
            satiation.CurrentThresholdDamage is { } damage)
        {
            _damageable.TryChangeDamage(uid, damage, true, false);
        }
    }

    private SatiationThreashold GetThreshold(Satiation satiation, float? level = null)
    {
        level ??= satiation.Current;
        var result = SatiationThreashold.Dead;
        var value = satiation.Prototype.Thresholds[SatiationThreashold.Full];
        foreach (var threshold in satiation.Prototype.Thresholds)
        {
            if (threshold.Value <= value && threshold.Value >= level)
            {
                result = threshold.Key;
                value = threshold.Value;
            }
        }
        return result;
    }

    /// <summary>
    /// A check that returns if the entity is below a satiation threshold.
    /// </summary>
    public bool IsSatiationBelowState(Entity<SatiationComponent?> ent, string type, SatiationThreashold threshold, float? thirst = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false; // It's never going to go unsatiated, so it's probably fine to assume that it's satiated.

        return GetThreshold(ent.Comp.Satiations[type], thirst) < threshold;
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

    private bool TryGetStatusIconPrototype(Satiation satiation, [NotNullWhen(true)] out StatusIconPrototype? prototype)
    {
        switch (satiation.CurrentThreshold)
        {
            case SatiationThreashold.Full:
                prototype = satiation.Prototype.Icons?[0].Item2;
                break;
            case SatiationThreashold.Concerned:
                prototype = satiation.Prototype.Icons?[1].Item2;
                break;
            case SatiationThreashold.Desperate:
                prototype = satiation.Prototype.Icons?[2].Item2;
                break;
            default:
                prototype = null;
                break;
        }

        return prototype != null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SatiationComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime < component.NextUpdateTime)
                continue;
            component.NextUpdateTime = _timing.CurTime + component.UpdateRate;

            foreach (var (_, satiation) in component.Satiations)
            {
                ModifySatiation(satiation, -satiation.ActualDecayRate);
                DoContinuousEffects(uid, satiation);
            }
        }
    }
}

