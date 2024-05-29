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
            if (!_prototype.TryIndex<SatiationPrototype>(satiation.Prototype, out var proto))
                continue;

            var amount = _random.Next(
                (int) proto.Thresholds[SatiationThreashold.Concerned] + 10,
                (int) proto.Thresholds[SatiationThreashold.Okay]);
            SetSatiation(satiation, amount);
            UpdateCurrentThreshold(uid, satiation);
            DoThresholdEffects(uid, satiation, false);

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
            if (!_prototype.TryIndex<SatiationPrototype>(satiation.Prototype, out var proto))
                continue;
            _alerts.ClearAlertCategory(uid, proto.AlertCategory);
        }
    }

    private void OnRefreshMovespeed(EntityUid uid, SatiationComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (_jetpack.IsUserFlying(uid))
            return;

        foreach(var (_, satiation) in component.Satiations)
        {
            if (!_prototype.TryIndex<SatiationPrototype>(satiation.Prototype, out var proto))
                continue;

            args.ModifySpeed(proto.SlowdownModifier, proto.SlowdownModifier);
        }
    }

    private void OnRejuvenate(EntityUid uid, SatiationComponent component, RejuvenateEvent args)
    {
        foreach(var (_, satiation) in component.Satiations)
        {
            if (!_prototype.TryIndex<SatiationPrototype>(satiation.Prototype, out var proto))
                continue;

            SetSatiation(satiation, proto.Thresholds[SatiationThreashold.Okay]);
        }
    }

    public void ModifySatiation(Entity<SatiationComponent?> ent, ProtoId<SatiationTypePrototype> satiationType, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (!ent.Comp.Satiations.TryGetValue(satiationType, out var satiation))
            return;

        SetSatiation(satiation, satiation.Current + amount);
    }

    private void SetSatiation(Satiation satiation, float amount)
    {
        if (!_prototype.TryIndex<SatiationPrototype>(satiation.Prototype, out var proto))
            return;

        satiation.Current = Math.Clamp(amount,
            proto.Thresholds[SatiationThreashold.Dead],
            proto.Thresholds[SatiationThreashold.Full]);
    }

    private void UpdateCurrentThreshold(EntityUid uid, Satiation satiation)
    {
            if (!_prototype.TryIndex<SatiationPrototype>(satiation.Prototype, out var proto))
                return;

            var calculatedNutritionThreshold = GetThreshold(satiation);
            if (calculatedNutritionThreshold == satiation.CurrentThreshold)
                return;
            satiation.CurrentThreshold = calculatedNutritionThreshold;
            if (proto.ThresholdDamage.TryGetValue(satiation.CurrentThreshold, out var damage))
                satiation.CurrentThresholdDamage = damage;
            else
                satiation.CurrentThresholdDamage = null;
            DoThresholdEffects(uid, satiation);
    }

    private bool DoThresholdEffects(EntityUid uid, Satiation satiation, bool force = false)
    {
        if (!_prototype.TryIndex<SatiationPrototype>(satiation.Prototype, out var proto))
            return false;

        if (satiation.CurrentThreshold == satiation.LastThreshold && !force)
            return false;

        if (GetMovementThreshold(satiation.CurrentThreshold) != GetMovementThreshold(satiation.LastThreshold))
        {
            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
        }
        if (proto.ThresholdDecayModifiers.TryGetValue(satiation.CurrentThreshold, out var modifier))
        {
            satiation.ActualDecayRate = satiation.BaseDecayRate * modifier;
        }
        satiation.LastThreshold = satiation.CurrentThreshold;

        if (proto.Alerts.TryGetValue(satiation.CurrentThreshold, out var alertId))
        {
            _alerts.ShowAlert(uid, alertId);
        }
        else
        {
            _alerts.ClearAlertCategory(uid, proto.AlertCategory);
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

        if (!_prototype.TryIndex<SatiationPrototype>(satiation.Prototype, out var proto))
            return result;

        var value = proto.Thresholds[SatiationThreashold.Full];
        foreach (var threshold in proto.Thresholds)
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
    public bool IsSatiationBelowState(Entity<SatiationComponent?> ent, ProtoId<SatiationTypePrototype> satiationType, SatiationThreashold threshold, float? thirst = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false; // It's never going to go unsatiated, so it's probably fine to assume that it's satiated.

        if (!ent.Comp.Satiations.TryGetValue(satiationType, out var satiation))
            return false; // It's never going to go unsatiated, so it's probably fine to assume that it's satiated.

        if (satiation == null)
            return false;

        return GetThreshold(satiation, thirst) < threshold;
    }
    ///
    /// <summary>
    /// A check that returns if the entity is below a satiation threshold.
    /// </summary>
    public bool IsCurrentSatiationBelowState(Entity<SatiationComponent?> ent, ProtoId<SatiationTypePrototype> satiationType, SatiationThreashold threshold, float delta = 0)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false; // It's never going to go unsatiated, so it's probably fine to assume that it's satiated.

        if (!ent.Comp.Satiations.TryGetValue(satiationType, out var satiation))
            return false; // It's never going to go unsatiated, so it's probably fine to assume that it's satiated.

        if (satiation == null)
            return false;

        return GetThreshold(satiation, satiation.Current + delta) < threshold;
    }

    public bool TryGetStatusIconPrototype(SatiationComponent component, ProtoId<SatiationTypePrototype> satiationType, [NotNullWhen(true)] out StatusIconPrototype? prototype)
    {
        prototype = null;

        if (!component.Satiations.TryGetValue(satiationType, out var satiation)
                || !_prototype.TryIndex(satiation.Prototype, out var satiationProto))
            return false;

        if (_prototype.TryIndex<StatusIconPrototype>(satiationProto.Icons[satiation.CurrentThreshold], out var iconProto))
            prototype = iconProto;

        return prototype != null;
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SatiationComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime < component.NextUpdateTime)
                continue;
            component.NextUpdateTime = _timing.CurTime + component.UpdateRate;

            foreach (var (satiationType, satiation) in component.Satiations)
            {
                ModifySatiation((uid, component), satiationType, -satiation.ActualDecayRate);
                DoContinuousEffects(uid, satiation);
            }
        }
    }
}

