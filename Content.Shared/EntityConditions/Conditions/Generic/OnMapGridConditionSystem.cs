using Content.Shared.Conditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <summary>
/// Returns true if griduid and mapuid match (AKA on 'planet').
/// </summary>
public sealed partial class OnMapGridConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<TransformComponent> entity, ref ConditionEvaluationEvent<OnMapGridCondition> args)
    {
        args.Handled = true;
        args.Value = (entity.Comp.GridUid == entity.Comp.MapUid && entity.Comp.MapUid != null) ? 1 : 0;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class OnMapGridCondition : EntityConditionBase<OnMapGridCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
