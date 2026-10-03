namespace Content.Shared.Conditions.UnifiedConditions;

public interface IOnMapGridCondition : ICondition<IOnMapGridCondition>
{
}

/// <summary>
/// Returns true if griduid and mapuid match (AKA on 'planet').
/// </summary>
public sealed partial class OnMapGridConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<TransformComponent> entity, ref ConditionEvaluationEvent<IOnMapGridCondition> args)
    {
        args.Handled = true;
        args.Value = entity.Comp.GridUid == entity.Comp.MapUid && entity.Comp.MapUid != null ? 1 : 0;
    }
}
