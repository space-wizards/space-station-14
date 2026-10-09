namespace Content.Shared.Conditions.UnifiedConditions;

public interface IOnMapGridCondition : ICondition
{
}

/// <summary>
/// Returns true if griduid and mapuid match (AKA on 'planet').
/// </summary>
public sealed partial class OnMapGridConditionSystem : ConditionEvaluatorSystem<IOnMapGridCondition>
{
    public override float Evaluate(IOnMapGridCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (!TryComp(entityUid, out TransformComponent? transform))
            return 0;
        return transform.GridUid == transform.MapUid && transform.MapUid != null ? 1 : 0;
    }
}
