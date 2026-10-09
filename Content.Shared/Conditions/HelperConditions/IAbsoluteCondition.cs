namespace Content.Shared.Conditions.HelperConditions;

/// <summary>
/// This condition will always return a given value.
/// Use to construct arbitrary equations using arithmetic conditions.
/// </summary>
public interface IAbsoluteCondition : ICondition
{
    float Value { get; }
}

/// <summary>
/// System for evaluating <see cref="IAbsoluteCondition" />
/// </summary>
public sealed partial class AbsoluteConditionSystem : ConditionEvaluatorSystem<IAbsoluteCondition>
{
    public override float Evaluate(IAbsoluteCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        return condition.Value;
    }
}
