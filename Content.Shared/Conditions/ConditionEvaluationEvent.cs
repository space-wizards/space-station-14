namespace Content.Shared.Conditions;

/// <summary>
/// Weakly Typed Event To Evaluate a Condition.
/// </summary>
/// <param name="condition">The condition to be used.</param>
/// <param name="entityUid">The entity for which we check the condition</param>
/// <param name="sourceEntity">An optional entity, which triggered this evaluation</param>
[ByRefEvent]
public abstract class ConditionEvaluationEvent(ICondition condition, EntityUid entityUid, EntityUid? sourceEntity)
{
    /// <summary>
    /// The entity for which we check the condition
    /// </summary>
    public EntityUid EntityUid { get; } = entityUid;

    /// <summary>
    /// An optional entity, which triggered this evaluation
    /// </summary>
    public EntityUid? SourceEntity { get; } = sourceEntity;

    /// <summary>
    /// The Value to which this conditions evaluate
    /// </summary>
    public float Value { get; set; }

    /// <summary>
    /// Must be set true by a handler, so that we know the condition can actually be evaluated.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// The condition to be used. A Handler using this, must do its own check for match.
    /// </summary>
    public ICondition ConditionWeak { get; } = condition;
}

/// <summary>
/// Strongly Typed event to connect <see cref="SharedConditionEvaluationSystem" /> to specific evaluator systems.
/// </summary>
/// <param name="condition">The condition to be used.</param>
/// <param name="entityUid">The entity for which we check the condition</param>
/// <param name="sourceEntity">An optional entity, which triggered this evaluation</param>
[ByRefEvent]
public sealed class ConditionEvaluationEvent<TCondition>(
    TCondition condition,
    EntityUid entityUid,
    EntityUid? sourceEntity)
    : ConditionEvaluationEvent(condition, entityUid, sourceEntity) where TCondition : ICondition
{
    /// <summary>
    /// The strongly typed condition to be used. A handler using this has certainty.
    /// </summary>
    public TCondition Condition { get; } = condition;
}
