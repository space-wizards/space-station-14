namespace Content.Shared.Conditions.HelperConditions;

/// <summary>
/// A helper condition wraps an inner condition to clamp its output value.
/// </summary>
public interface ILimiterCondition : ICondition<ILimiterCondition>
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
public sealed partial class LimiterConditionSystem : EntitySystem
{
    [Dependency] private SharedConditionEvaluationSystem _conditionEvaluationSystem = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<MetaDataComponent> _, ref ConditionEvaluationEvent<ILimiterCondition> args)
    {
        var innerValue =
            _conditionEvaluationSystem.EvaluateCondition(args.Condition.Condition, args.EntityUid, args.SourceEntity);
        args.Value = MathHelper.Clamp(innerValue,
            args.Condition.MinimumOutputValue,
            args.Condition.MaximumOutputValue);
    }
}
