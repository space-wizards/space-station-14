namespace Content.Shared.Conditions.UnifiedConditions;

public interface IInSpaceCondition : ICondition
{
}

/// <summary>
/// Returns true if the entity is in space.
/// </summary>
public sealed partial class InSpaceConditionSystem : ConditionEvaluatorSystem<IInSpaceCondition>
{
    public override float Evaluate(IInSpaceCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (!TryComp(entityUid, out TransformComponent? transformComponent))
            return 0;
        return transformComponent.GridUid == null ? 1 : 0;
    }
}
