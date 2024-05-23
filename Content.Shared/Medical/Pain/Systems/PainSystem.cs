using Content.Shared.FixedPoint;
using Content.Shared.Medical.HealthConditions.Components;
using Content.Shared.Medical.HealthConditions.Systems;
using Content.Shared.Medical.Pain.Components;
using Content.Shared.Medical.Pain.Events;

namespace Content.Shared.Medical.Pain.Systems;


public sealed class PainSystem : EntitySystem
{
    [Dependency] private readonly HealthConditionSystem _healthConditionSystem = default!;

    public void CheckPainThresholds(Entity<NervesComponent?> nervousSystem)
    {
        if (!Resolve(nervousSystem,ref nervousSystem.Comp))
            return;
        var pain = nervousSystem.Comp.Pain;
        var changedThresholds = new List<(FixedPoint2, NervesComponent.MedicalConditionThreshold)>();
        var conditionManager = new Entity<HealthConditionManagerComponent?>(nervousSystem, null);
        foreach (var (painPercent, conditionData) in nervousSystem.Comp.ConditionThresholds)
        {
            var requiredPain = painPercent / 100 * nervousSystem.Comp.MaxPain;
            requiredPain = CalculateAdjustedPain(new(nervousSystem, nervousSystem.Comp), requiredPain);
            if (requiredPain < pain)
            {
                if (conditionData.Applied)
                {
                    _healthConditionSystem.TryRemoveCondition(conditionManager, conditionData.ConditionId);
                    changedThresholds.Add((painPercent, conditionData with {Applied = false}));
                }
                continue; //move next
            }
            if (!conditionData.Applied)
                continue;

            _healthConditionSystem.TryAddCondition(conditionManager, conditionData.ConditionId,
                out _, 100);
            changedThresholds.Add((painPercent, conditionData with {Applied = true}));
        }

        foreach (var (painThreshold, newThresholdData) in changedThresholds)
        {
            nervousSystem.Comp.ConditionThresholds[painThreshold] = newThresholdData;
        }
        Dirty(nervousSystem, nervousSystem.Comp);
    }

    public FixedPoint2 CalculateAdjustedPain(Entity<NervesComponent> nervousSystem, FixedPoint2 rawPain)
    {
        return FixedPoint2.Clamp(rawPain * nervousSystem.Comp.Multiplier - nervousSystem.Comp.MitigatedPain, 0,
            nervousSystem.Comp.PainCap);
    }

    public void ChangePain(Entity<NervesComponent?> nervousSystem, FixedPoint2 painDelta)
    {
        if (painDelta == 0 || !Resolve(nervousSystem,ref nervousSystem.Comp))
            return;
        var oldPain = nervousSystem.Comp.Pain;
        nervousSystem.Comp.RawPain += painDelta;
        var newPain = nervousSystem.Comp.Pain;
        Dirty(nervousSystem);
        if (oldPain == newPain)
            return;
        var ev = new PainChangedEvent(new(nervousSystem, nervousSystem.Comp), newPain - oldPain);
        RaiseLocalEvent(nervousSystem, ref ev);
        CheckPainThresholds(nervousSystem);
    }

    public void ChangeCap(Entity<NervesComponent?> nervousSystem, FixedPoint2 painCapDelta)
    {
        if (painCapDelta == 0 || !Resolve(nervousSystem,ref nervousSystem.Comp))
            return;
        var oldPain = nervousSystem.Comp.Pain;
        nervousSystem.Comp.RawPainCap += painCapDelta;
        var newPain = nervousSystem.Comp.Pain;
        Dirty(nervousSystem);
        if (oldPain == newPain)
            return;
        var ev = new PainChangedEvent(new(nervousSystem, nervousSystem.Comp), newPain - oldPain);
        RaiseLocalEvent(nervousSystem, ref ev);
        CheckPainThresholds(nervousSystem);
    }

    public void ChangeMultiplier(Entity<NervesComponent?> nervousSystem, FixedPoint2 painMultDelta)
    {
        if (painMultDelta == 0 || !Resolve(nervousSystem,ref nervousSystem.Comp))
            return;
        var oldPain = nervousSystem.Comp.Pain;
        nervousSystem.Comp.Multiplier += painMultDelta;
        var newPain = nervousSystem.Comp.Pain;
        Dirty(nervousSystem);
        if (oldPain == newPain)
            return;
        var ev = new PainChangedEvent(new(nervousSystem, nervousSystem.Comp), newPain - oldPain);
        RaiseLocalEvent(nervousSystem, ref ev);
        CheckPainThresholds(nervousSystem);
    }

    public void ChangeMitigation(Entity<NervesComponent?> nervousSystem, FixedPoint2 mitigationPercentDelta)
    {
        if (mitigationPercentDelta == 0 || !Resolve(nervousSystem,ref nervousSystem.Comp))
            return;
        var oldPain = nervousSystem.Comp.Pain;
        nervousSystem.Comp.RawMitigatedPercentage += mitigationPercentDelta;
        var newPain = nervousSystem.Comp.Pain;
        Dirty(nervousSystem);
        if (oldPain == newPain)
            return;
        var ev = new PainChangedEvent(new(nervousSystem, nervousSystem.Comp), newPain - oldPain);
        RaiseLocalEvent(nervousSystem, ref ev);
        CheckPainThresholds(nervousSystem);
    }
}
