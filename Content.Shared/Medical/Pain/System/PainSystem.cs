using Content.Shared.FixedPoint;
using Content.Shared.Medical.Consciousness.Components;
using Content.Shared.Medical.Consciousness.Systems;
using Content.Shared.Medical.Pain.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Pain.System;

public sealed class PainSystem : EntitySystem
{
    [Dependency] private ConsciousnessSystem _consciousnessSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PainThresholdsComponent, ComponentGetState>(OnPainThresholdGetState);
        SubscribeLocalEvent<PainThresholdsComponent, ComponentHandleState>(OnPainThresholdHandleState);

        SubscribeLocalEvent<PainInflicterComponent, ComponentGetState>(OnPainInflicterGetState);
        SubscribeLocalEvent<PainInflicterComponent, ComponentHandleState>(OnPainInflicterHandleState);
    }

    private void OnPainInflicterHandleState(EntityUid uid, PainInflicterComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not PainInflicterComponentState state)
            return;
        component.Pain = state.Pain;
    }

    private void OnPainInflicterGetState(EntityUid uid, PainInflicterComponent component, ref ComponentGetState args)
    {
        args.State = new PainInflicterComponentState(
            component.Pain
        );
    }

    private void OnPainThresholdHandleState(EntityUid uid, PainThresholdsComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not PainThresholdComponentState state)
            return;
        component.Thresholds = new SortedDictionary<FixedPoint2, FixedPoint2>(state.Thresholds);
        component.PainModifier = state.PainModifier;
        component.RawPain = state.RawPain;
        CheckThresholds(uid, component);
    }

    private void OnPainThresholdGetState(EntityUid uid, PainThresholdsComponent component, ref ComponentGetState args)
    {
        args.State = new PainThresholdComponentState(
            component.PainModifier,
            component.RawPain,
            new Dictionary<FixedPoint2, FixedPoint2>(component.Thresholds)
        );
    }


    public FixedPoint2 GetPain(EntityUid target, PainThresholdsComponent? painThresholds = null)
    {
        if (!Resolve(target, ref painThresholds))
            return -1;
        return painThresholds.RawPain * painThresholds.PainModifier;
    }

    //Add or remove pain from a pain receiver
    public bool ApplyPain(EntityUid target, FixedPoint2 pain, PainThresholdsComponent? painThresholds = null,
        EntityUid? inflicterEntity = null)
    {
        if (pain == 0 || !Resolve(target, ref painThresholds))
            return false;

        var ev = new InflictPainEvent(painThresholds, pain, inflicterEntity);
        RaiseLocalEvent(target, ref ev, true);
        if (ev.Canceled)
            return false;
        painThresholds.RawPain += pain;
        Dirty(target);
        CheckThresholds(target, painThresholds);
        return true;
    }


    private FixedPoint2 GetPreviousConsciousnessDamage(PainThresholdsComponent painThresholds)
    {
        return painThresholds.CurrentThreshold == 0 ? 0 : painThresholds.Thresholds[painThresholds.CurrentThreshold];
    }

    private FixedPoint2 GetConsciousnessDamageForPain(PainThresholdsComponent painThresholds, FixedPoint2 pain, out FixedPoint2 threshold)
    {
        FixedPoint2 foundDamage = 0;
        threshold = 0;
        foreach (var (painThreshold, consciousnessDamage) in painThresholds.Thresholds)
        {
            if (pain < painThreshold)
                break;
            threshold = painThreshold;
            foundDamage = consciousnessDamage;
        }
        return foundDamage;
    }

    private void CheckThresholds(EntityUid target, PainThresholdsComponent? painThresholds)
    {
        if (!Resolve(target,ref painThresholds))
            return;
        var pain = painThresholds.RawPain * painThresholds.PainModifier;
        var delta = (GetPreviousConsciousnessDamage(painThresholds) - GetConsciousnessDamageForPain(painThresholds, pain, out var newThreshold));
        if (newThreshold == painThresholds.CurrentThreshold)
            return;
        if (newThreshold < painThresholds.CurrentThreshold)
        {
            _consciousnessSystem.AddToDamage(target, -delta);
        }
        else
        {
            _consciousnessSystem.AddToDamage(target, delta);
        }
        painThresholds.CurrentThreshold = newThreshold;
    }
}
