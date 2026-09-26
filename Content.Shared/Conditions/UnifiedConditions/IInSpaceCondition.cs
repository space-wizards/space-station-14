namespace Content.Shared.Conditions.UnifiedConditions;

public interface IInSpaceCondition : ICondition<IInSpaceCondition>
{
}

/// <summary>
/// Returns true if the entity is in space.
/// </summary>
public sealed partial class InSpaceConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<TransformComponent> entity, ref ConditionEvaluationEvent<IInSpaceCondition> args)
    {
        args.Handled = true;
        args.Value = entity.Comp.GridUid == null ? 1 : 0;
    }
}
