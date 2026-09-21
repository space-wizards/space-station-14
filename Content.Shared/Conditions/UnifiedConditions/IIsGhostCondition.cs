using Content.Shared.Ghost.Components;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IIsGhostCondition : ICondition<IIsGhostCondition>
{

}

/// <summary>
/// Returns true if the entity is a ghost.
/// </summary>
public sealed partial class IsGhostConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<TransformComponent> entity, ref ConditionEvaluationEvent<IIsGhostCondition> args)
    {
        args.Handled = true;

        args.Value = HasComp<GhostComponent>(entity) ? 1 : 0;
    }
}
