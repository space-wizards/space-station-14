namespace Content.Shared.Conditions.HelperConditions;

/// <summary>
/// A flattening condition that unlike <see cref="IMultiplierCondition"/> only produces a binary value from its children.
/// </summary>
public interface IAnyCondition : IConditionByEvent<IAnyCondition>
{
    IEnumerable<ICondition> Conditions { get; }
}

/// <summary>
/// Returns true if this solution entity has an amount of reagent in it within a specified minimum and maximum.
/// </summary>
public sealed partial class AnyConditionSystem : EntitySystem
{
    [Dependency] private SharedConditionEvaluationSystem _sharedConditionEvaluationSystem = default!;

    [SubscribeLocalEvent]
    private void Condition(Entity<MetaDataComponent> entity,
        ref ConditionEvaluationEvent<IAnyCondition> args)
    {
        args.Handled = true;
        foreach (var cnd in args.Condition.Conditions)
        {
            if (_sharedConditionEvaluationSystem.IsConditionSatisfied(cnd, entity, args.SourceEntity))
            {
                args.Value = 1;
                return;
            }
        }
    }
}
