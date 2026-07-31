using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <summary>
/// Returns true if griduid and mapuid match (AKA on 'planet').
/// </summary>
public sealed class OnMapGridConditionSystem : EntityConditionSystem<TransformComponent, OnMapGridCondition>
{
    protected override void Condition(Entity<TransformComponent> entity, ref EntityConditionEvent<OnMapGridCondition> args)
    {
        args.Result = entity.Comp.GridUid == entity.Comp.MapUid && entity.Comp.MapUid != null;
    }
}


/// <inheritdoc cref="EntityCondition"/>
public sealed partial class OnMapGridCondition : EntityConditionBase<OnMapGridCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
