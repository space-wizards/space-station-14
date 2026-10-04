namespace Content.Shared.Conditions.HelperConditions;

/// <summary>
/// A flattening condition that unlike <see cref="IMultiplierCondition" /> only produces a binary value from its children.
/// </summary>
public interface IAllCondition : ICondition
{
    IEnumerable<ICondition> Conditions { get; }
}

/// <summary>
/// Returns true if this solution entity has an amount of reagent in it within a specified minimum and maximum.
/// </summary>
public sealed partial class AllConditionSystem : ConditionEvaluatorSystem<IAllCondition>
{
    [Dependency] private SharedConditionEvaluationSystem _sharedConditionEvaluationSystem = default!;

    public override float Evaluate(IAllCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        foreach (var cnd in condition.Conditions)
        {
            if (!_sharedConditionEvaluationSystem.IsConditionSatisfied(cnd, entityUid, sourceEntity))
            {
                return 0;
            }
        }
        return 1;
    }
}
