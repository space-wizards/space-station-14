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

    private void OnMapInit(Entity<SatiationComponent> ent, ref MapInitEvent args)
    {
        foreach (var (_, satiation) in ent.Comp.Satiations)
        {
            var proto = _prototype.Index<SatiationPrototype>(satiation.Prototype);

            var amount = _random.Next(
                (int) proto.Thresholds[SatiationThreashold.Concerned] + 10,
                (int) proto.Thresholds[SatiationThreashold.Okay]);
            SetSatiation(ent, satiation, amount);
            UpdateCurrentThreshold(ent, satiation);
            DoThresholdEffects(ent.Owner, satiation, true);
        }
        Dirty(ent);
    }

    private void OnShutdown(Entity<SatiationComponent> ent, ref ComponentShutdown args)
    {
        foreach(var (_, satiation) in ent.Comp.Satiations)
        {
            if (!_prototype.TryIndex<SatiationPrototype>(satiation.Prototype, out var proto))
                continue;
            _alerts.ClearAlertCategory(ent.Owner, proto.AlertCategory);
        }
    }

    private void OnRefreshMovespeed(Entity<SatiationComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (_jetpack.IsUserFlying(ent.Owner))
            return;

        foreach(var (_, satiation) in ent.Comp.Satiations)
        {
            if (satiation.CurrentThreshold > SatiationThreashold.Concerned)
                continue;

            if (!_prototype.TryIndex<SatiationPrototype>(satiation.Prototype, out var proto))
                continue;

            args.ModifySpeed(proto.SlowdownModifier, proto.SlowdownModifier);
        }
    }

    private void OnRejuvenate(Entity<SatiationComponent> ent, ref RejuvenateEvent args)
    {
        foreach(var (_, satiation) in ent.Comp.Satiations)
        {
            if (!_prototype.TryIndex<SatiationPrototype>(satiation.Prototype, out var proto))
                continue;

            SetSatiation(ent, satiation, proto.Thresholds[SatiationThreashold.Okay]);
        }
        Dirty(ent);
    }

    public void ModifySatiation(Entity<SatiationComponent?> ent, ProtoId<SatiationTypePrototype> satiationType, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (!ent.Comp.Satiations.TryGetValue(satiationType, out var satiation))
            return;

        SetSatiation((ent.Owner, ent.Comp), satiation, satiation.Current + amount);
    }

    public void SetSatiation(Entity<SatiationComponent?> ent, ProtoId<SatiationTypePrototype> satiationType, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (!ent.Comp.Satiations.TryGetValue(satiationType, out var satiation))
            return;

        SetSatiation((ent.Owner, ent.Comp), satiation, amount);
    }

    private void SetSatiation(Entity<SatiationComponent> ent, Satiation satiation, float amount)
    {
        if (!_prototype.TryIndex<SatiationPrototype>(satiation.Prototype, out var proto))
            return;

        satiation.Current = Math.Clamp(amount,
            proto.Thresholds[SatiationThreashold.Dead],
            proto.Thresholds[SatiationThreashold.Full]);

        UpdateCurrentThreshold((ent.Owner, ent.Comp), satiation);
        Dirty(ent);
    }

    private void UpdateCurrentThreshold(Entity<SatiationComponent> ent, Satiation satiation)
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
        DoThresholdEffects(ent.Owner, satiation);
        Dirty(ent);
    }

    private void DoThresholdEffects(EntityUid uid, Satiation satiation, bool force = false)
    {
        if (!_prototype.TryIndex<SatiationPrototype>(satiation.Prototype, out var proto))
            return;

        if (satiation.CurrentThreshold == satiation.LastThreshold && !force)
            return;

        if (GetMovementThreshold(satiation.CurrentThreshold) != GetMovementThreshold(satiation.LastThreshold))
        {
            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
        }
        if (proto.ThresholdDecayModifiers.TryGetValue(satiation.CurrentThreshold, out var modifier))
        {
            satiation.ActualDecayRate = proto.BaseDecayRate * modifier;
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
    }

    private void DoContinuousEffects(EntityUid uid, Satiation satiation)
    {
        if (!_mobState.IsDead(uid) &&
            satiation.CurrentThresholdDamage is { } damage)
        {
            _damageable.TryChangeDamage(uid, damage, true, false);
        }
    }

    public bool TryGetSatiationThreshold(Entity<SatiationComponent?> ent, ProtoId<SatiationTypePrototype> satiationType, [NotNullWhen(true)] out SatiationThreashold? threshold, float? level = null)
    {
        threshold = null;

        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (!ent.Comp.Satiations.TryGetValue(satiationType, out var satiation))
            return false;

        threshold = GetThreshold(satiation, level);
        return true;
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

    public bool TryGetSatiationPrototype(Entity<SatiationComponent?> ent, ProtoId<SatiationTypePrototype> satiationType, [NotNullWhen(true)] out ProtoId<SatiationPrototype>? satiationPrototype)
    {
        satiationPrototype = null;

        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (!ent.Comp.Satiations.TryGetValue(satiationType, out var satiation))
            return false;

        satiationPrototype = satiation.Prototype;
        return true;
    }


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

    public bool TryGetStatusIconPrototype(Entity<SatiationComponent?> ent, ProtoId<SatiationTypePrototype> satiationType, [NotNullWhen(true)] out StatusIconPrototype? prototype)
    {
        prototype = null;

        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (!ent.Comp.Satiations.TryGetValue(satiationType, out var satiation)
                || !_prototype.TryIndex(satiation.Prototype, out var satiationProto))
            return false;

        if (_prototype.TryIndex<StatusIconPrototype>(satiationProto.Icons[satiation.CurrentThreshold], out var iconProto))
            prototype = iconProto;

        return prototype != null;
    }

    public bool TryGetCurrentSatiation(Entity<SatiationComponent?> ent, ProtoId<SatiationTypePrototype> satiationType, [NotNullWhen(true)] out float? current)
    {
        current = null;

        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (!ent.Comp.Satiations.TryGetValue(satiationType, out var satiation))
            return false;

        current = satiation.Current;
        return true;
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

