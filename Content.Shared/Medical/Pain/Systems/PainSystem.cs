using Content.Shared.FixedPoint;
using Content.Shared.Medical.Consciousness.Systems;
using Content.Shared.Medical.HealthConditions.Components;
using Content.Shared.Medical.HealthConditions.Systems;
using Content.Shared.Medical.Pain.Components;

namespace Content.Shared.Medical.Pain.Systems;


public sealed class PainSystem : EntitySystem
{

    [Dependency] private readonly ConsciousnessSystem _consciousnessSystem = default!;
    [Dependency] private readonly HealthConditionSystem _healthConditionSystem = default!;


    public void CheckPainThresholds(Entity<NervousSystemComponent?> nervousSystem)
    {
        if (!Resolve(nervousSystem, ref nervousSystem.Comp))
            return;
        var pain = nervousSystem.Comp.Pain;
        if (pain > nervousSystem.Comp.UnConsciousThresholdPain
            && !nervousSystem.Comp.UnConsciousnessApplied)
        {
            nervousSystem.Comp.UnConsciousnessApplied = true;
            //TODO: cause unconsious
        }


        var changedThresholds = new List<(FixedPoint2, bool)>();
        var conditionManager = new Entity<HealthConditionManagerComponent?>(nervousSystem, null);
        foreach (var (painPercent, conditionData) in nervousSystem.Comp.ConditionThresholds)
        {
            var requiredPain = painPercent / 100 * nervousSystem.Comp.NominalMaxPain;
            if (requiredPain < nervousSystem.Comp.Pain)
            {
                if (conditionData.Applied)
                {
                    _healthConditionSystem.TryRemoveCondition(conditionManager, conditionData.ConditionId);
                    changedThresholds.Add((painPercent, false));
                }
                continue; //move next
            }

            if (conditionData.Applied)
            {
                _healthConditionSystem.TryAddCondition(conditionManager, conditionData.ConditionId, out var condition, 100);
                changedThresholds.Add((painPercent, false));
                continue;
            }

        }


        Dirty(nervousSystem, nervousSystem.Comp);
    }

}
