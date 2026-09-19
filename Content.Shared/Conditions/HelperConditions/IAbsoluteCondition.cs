namespace Content.Shared.Conditions.HelperConditions;

/// <summary>
/// This condition will always return a given value.
/// Use to construct arbitrary equations using arithmetic conditions.
/// </summary>
public interface IAbsoluteCondition : ICondition<IAbsoluteCondition>
{
    float Value { get; }
}

/// <summary>
/// System for evaluating <see cref="IAbsoluteCondition"/>
/// </summary>
public  sealed partial class AbsoluteConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<MetaDataComponent> _, ref ConditionEvaluationEvent<IAbsoluteCondition> args)
    {
        args.Handled = true;
        args.Value = args.Condition.Value;
    }

}
