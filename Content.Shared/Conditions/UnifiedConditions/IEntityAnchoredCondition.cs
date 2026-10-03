namespace Content.Shared.Conditions.UnifiedConditions;

public interface IEntityAnchoredCondition : IConditionByEvent<IEntityAnchoredCondition>
{
}

public sealed partial class EntityAnchoredConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<TransformComponent> entity,
        ref ConditionEvaluationEvent<IEntityAnchoredCondition> args)
    {
        args.Handled = true;
        args.Value = entity.Comp.Anchored ? 1 : 0;
    }

    /*
     *
    public bool Condition(EntityUid uid, IEntityManager entityManager)
           {
               var transform = entityManager.GetComponent<TransformComponent>(uid);
               return transform.Anchored && Anchored || !transform.Anchored && !Anchored;
           }
     *
     */
}
