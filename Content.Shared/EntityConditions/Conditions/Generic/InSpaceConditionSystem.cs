using Content.Shared.Conditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <summary>
/// Returns true if the entity is in space.
/// </summary>
public sealed partial class InSpaceConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<TransformComponent> entity, ref ConditionEvaluationEvent<InSpaceCondition> args)
    {
        args.Handled = true;
        args.Value = entity.Comp.GridUid == null ? 1 : 0;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class InSpaceCondition : EntityConditionBase<InSpaceCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
