using System.Linq;

namespace Content.Shared.Conditions.HelperConditions;

/// <summary>
/// A helper condition that returns the multiple of its inner conditions.
/// </summary>
public interface IMultiplierCondition : ICondition<IMultiplierCondition>
{
    /// <summary>
    /// Conditions multiplied together.
    /// </summary>
    IEnumerable<ICondition> Multipliers { get; }
}

/// <summary>
/// Evaluation of <see cref="IMultiplierCondition"/>
/// </summary>
public sealed partial class  MultiplierConditionSystem : EntitySystem
{
    [Dependency] private SharedConditionEvaluationSystem _conditionEvaluationSystem = default!;

    [SubscribeLocalEvent]
    private void Condition(ref ConditionEvaluationEvent<IMultiplierCondition> args)
    {
        var entity = args.EntityUid;
        var sourceEntity = args.SourceEntity;
        if (args.Condition.Multipliers.Any())
        {
            args.Value = args.Condition.Multipliers
                .Select(e => _conditionEvaluationSystem.EvaluateCondition(e, entity, sourceEntity))
                .Append(1)
                .Aggregate((a, b) => a * b);
        }
        else
        {
            args.Value = 0;
        }
    }
}
