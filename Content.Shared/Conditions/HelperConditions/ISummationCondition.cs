using System.Linq;

namespace Content.Shared.Conditions.HelperConditions;

/// <summary>
/// A helper condition, that just
/// </summary>
public interface ISummationCondition : ICondition<ISummationCondition>
{
    /// <summary>
    /// Conditions whose value will be summed together.
    /// </summary>
    IEnumerable<ICondition> Summands { get; }
}

/// <summary>
/// Evaluation of <see cref="ISummationCondition" />
/// </summary>
public sealed partial class SummationConditionSystem : EntitySystem
{
    [Dependency] private SharedConditionEvaluationSystem _conditionEvaluationSystem = default!;

    [SubscribeLocalEvent]
    private void Condition(ref ConditionEvaluationEvent<ISummationCondition> args)
    {
        var entity = args.EntityUid;
        var sourceEntity = args.SourceEntity;
        if (args.Condition.Summands.Any())
        {
            args.Value = args.Condition.Summands
                .Sum(e => _conditionEvaluationSystem.EvaluateCondition(e, entity, sourceEntity));
        }
        else
            args.Value = 0;
    }
}
