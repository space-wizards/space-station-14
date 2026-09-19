using Content.Shared.Conditions;
using Content.Shared.Ghost.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <summary>
/// Returns true if the entity is a ghost.
/// </summary>
public sealed partial class IsGhostConditionSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void Condition(Entity<TransformComponent> entity, ref ConditionEvaluationEvent<IsGhostCondition> args)
    {
        args.Handled = true;

        args.Value = HasComp<GhostComponent>(entity) ? 1 : 0;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class IsGhostCondition : EntityConditionBase<IsGhostCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
