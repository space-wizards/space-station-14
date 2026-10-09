using Content.Shared.Conditions.Satisfier;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IWiresCutCondition : ICondition, IConditionWithDefaultSatisfactionRule
{
    bool Value { get; }

    Satisfier.Satisfier IConditionWithDefaultSatisfactionRule.GetDefaultSatisfier()
    {
        return new WithThreshold
        {
            Comparison = WithThreshold.Comparator.GreaterEqual,
            Threshold = 1,
        };
    }
}
