using Content.Shared.FixedPoint;
using Content.Shared.Medical.Consciousness.Components;
using Content.Shared.Medical.Consciousness.Systems;
using Content.Shared.Medical.Pain.Components;

namespace Content.Shared.Medical.Pain.System;

public sealed class PainSystem : EntitySystem
{
    [Dependency] private ConsciousnessSystem _consciousnessSystem = default!;


    public FixedPoint2 GetPain(EntityUid target, PainThresholdsComponent? painThresholds = null)
    {
        if (!Resolve(target, ref painThresholds))
            return -1;
        return painThresholds.RawPain * painThresholds.PainModifier;
    }

    //Add or remove pain to a pain receiver
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
        return true;
    }

    private void CheckThresholds(EntityUid target, PainThresholdsComponent? painThresholds)
    {
        if (!Resolve(target,ref painThresholds))
            return;
        var pain = painThresholds.RawPain * painThresholds.PainModifier;
        foreach (var (painThreshold, consciousnessDamage) in painThresholds.Thresholds)
        {
            if (pain < painThreshold)
                break;
            if (painThreshold > consciousnessDamage)
                continue;
            _consciousnessSystem.AddToDamage(target, consciousnessDamage);
        }
    }


}
