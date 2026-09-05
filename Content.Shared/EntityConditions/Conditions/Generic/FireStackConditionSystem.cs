using Content.Shared.Atmos.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <summary>
/// Returns true if an entity has at least <see cref="FireStacks"/> fire stacks.
/// </summary>
public sealed partial class FireStackConditionSystem : EntityConditionSystem<FlammableComponent, FireStackCondition>
{
    protected override void Condition(Entity<FlammableComponent> entity, ref EntityConditionEvent<FireStackCondition> args)
    {
        args.Result = entity.Comp.FireStacks > args.Condition.FireStacks;
    }
}


/// <inheritdoc cref="EntityCondition"/>
public sealed partial class FireStackCondition : EntityConditionBase<FireStackCondition>
{
    [DataField]
    public float FireStacks = 0.2f;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
