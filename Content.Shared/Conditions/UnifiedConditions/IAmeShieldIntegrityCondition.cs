using Content.Shared.Conditions.Satisfier;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IAmeShieldIntegrityCondition : IConditionByEvent<IAmeShieldIntegrityCondition>,
    IConditionWithDefaultSatisfactionRule
{
    float IntegrityThreshold { get; }

    WithThreshold.Comparator Comparator { get; }

    Satisfier.Satisfier IConditionWithDefaultSatisfactionRule.GetDefaultSatisfier()
    {
        return new WithThreshold
        {
            // since in ConditionSystem -> Component.Integrity / IntegrityThreshold, our threshold here is 1
            Threshold = 1,
            Comparison = Comparator,
        };
    }
}
