using System.Linq;

namespace Content.Shared.Conditions.HelperConditions;

/// <summary>
/// A helper condition that returns the multiple of its inner conditions.
/// </summary>
public interface IMultiplierCondition : ICondition
{
    /// <summary>
    /// Conditions multiplied together.
    /// </summary>
    IEnumerable<ICondition> Multipliers { get; }
}

/// <summary>
/// Evaluation of <see cref="IMultiplierCondition" />
/// </summary>
public sealed partial class MultiplierConditionSystem : ConditionEvaluatorSystem<IMultiplierCondition>
{
    [Dependency] private SharedConditionEvaluationSystem _conditionEvaluationSystem = default!;

    public override float Evaluate(IMultiplierCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (condition.Multipliers.Any())
        {
            return condition.Multipliers
                .Select(e => _conditionEvaluationSystem.EvaluateCondition(e, entityUid, sourceEntity))
                .Append(1)
                .Aggregate((a, b) => a * b);
        }
        else
            return 0;
    }
}
