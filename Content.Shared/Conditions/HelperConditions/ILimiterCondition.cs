namespace Content.Shared.Conditions.HelperConditions;

/// <summary>
/// A helper condition wraps an inner condition to clamp its output value.
/// </summary>
public interface ILimiterCondition : ICondition
{
    ICondition Condition { get; }

    /// <summary>
    /// Lower boundary to which the value of condition will be capped at.
    /// </summary>
    float MinimumOutputValue { get; }

    /// <summary>
    /// Upper boundary to which the value of condition will be capped at.
    /// </summary>
    float MaximumOutputValue { get; }
}

/// <summary>
/// Evaluates <see cref="ILimiterCondition" />
/// </summary>
public sealed partial class LimiterConditionSystem : ConditionEvaluatorSystem<ILimiterCondition>
{
    [Dependency] private SharedConditionEvaluationSystem _conditionEvaluationSystem = default!;

    public override float Evaluate(ILimiterCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        var innerValue =
            _conditionEvaluationSystem.EvaluateCondition(condition.Condition, entityUid, sourceEntity);
         return MathHelper.Clamp(innerValue,
            condition.MinimumOutputValue,
            condition.MaximumOutputValue);
    }
}
