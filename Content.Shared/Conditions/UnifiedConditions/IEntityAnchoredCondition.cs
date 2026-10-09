namespace Content.Shared.Conditions.UnifiedConditions;

public interface IEntityAnchoredCondition : ICondition
{
}

public sealed partial class EntityAnchoredConditionSystem : ConditionEvaluatorSystem<IEntityAnchoredCondition>
{
    /*
     *
    public bool Condition(EntityUid uid, IEntityManager entityManager)
           {
               var transform = entityManager.GetComponent<TransformComponent>(uid);
               return transform.Anchored && Anchored || !transform.Anchored && !Anchored;
           }
     *
     */
    public override float Evaluate(IEntityAnchoredCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (!TryComp(entityUid, out TransformComponent? transformComponent))
            return 0;
        return transformComponent.Anchored ? 1 : 0;
    }
}
