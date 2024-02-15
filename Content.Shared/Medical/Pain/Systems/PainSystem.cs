using Content.Shared.FixedPoint;
using Content.Shared.Medical.Consciousness.Systems;
using Content.Shared.Medical.HealthConditions.Components;
using Content.Shared.Medical.HealthConditions.Systems;
using Content.Shared.Medical.Pain.Components;

namespace Content.Shared.Medical.Pain.Systems;


public sealed class PainSystem : EntitySystem
{
    [Dependency] private readonly HealthConditionSystem _healthConditionSystem = default!;

    public void CheckPainThresholds(Entity<NervousSystemComponent?> nervousSystem)
    {
        if (!Resolve(nervousSystem, ref nervousSystem.Comp))
            return;
        var pain = nervousSystem.Comp.Pain;

        var changedThresholds = new List<(FixedPoint2, NervousSystemComponent.MedicalConditionThreshold)>();
        var conditionManager = new Entity<HealthConditionManagerComponent?>(nervousSystem, null);
        foreach (var (painPercent, conditionData) in nervousSystem.Comp.ConditionThresholds)
        {
            var requiredPain = painPercent / 100 * nervousSystem.Comp.NominalMaxPain;
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

}
