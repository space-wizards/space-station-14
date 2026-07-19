using Content.Shared.Ghost;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <summary>
/// Returns true if the entity is a ghost.
/// </summary>
public sealed class IsGhostConditionSystem : EntityConditionSystem<TransformComponent, IsGhostCondition>
{
    protected override void Condition(Entity<TransformComponent> entity, ref EntityConditionEvent<IsGhostCondition> args)
    {
        args.Result = HasComp<GhostComponent>(entity);
    }
}


/// <inheritdoc cref="EntityCondition"/>
public sealed partial class IsGhostCondition : EntityConditionBase<IsGhostCondition>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
