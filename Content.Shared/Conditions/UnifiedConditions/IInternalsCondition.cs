using Content.Shared.Body.Components;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IInternalsCondition : ICondition<IInternalsCondition>
{

}

/// <summary>
/// Returns true if this entity is using internals. False if they are not or cannot use internals.
/// </summary>
public sealed partial class InternalsOnEntityConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<InternalsComponent> entity, ref ConditionEvaluationEvent<IInternalsCondition> args)
    {
        args.Handled = true;

        args.Value = entity.Comp.GasTankEntity != null ? 1 : 0;
    }
}
