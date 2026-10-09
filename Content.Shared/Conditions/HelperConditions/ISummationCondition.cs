using System.Linq;

namespace Content.Shared.Conditions.HelperConditions;

/// <summary>
/// A helper condition, that just
/// </summary>
public interface ISummationCondition : ICondition
{
    /// <summary>
    /// Conditions whose value will be summed together.
    /// </summary>
    IEnumerable<ICondition> Summands { get; }
}

/// <summary>
/// Evaluation of <see cref="ISummationCondition" />
/// </summary>
public sealed partial class SummationConditionSystem : ConditionEvaluatorSystem<ISummationCondition>
{
    [Dependency] private SharedConditionEvaluationSystem _conditionEvaluationSystem = default!;

    public override float Evaluate(ISummationCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (condition.Summands.Any())
        {
            return condition.Summands
                .Sum(e => _conditionEvaluationSystem.EvaluateCondition(e, entityUid, sourceEntity));
        }

        return 0;
    }
}
