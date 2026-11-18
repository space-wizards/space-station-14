using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Generic;

/// <summary>
/// Checks whether the entity has all components.
/// </summary>
public sealed partial class HasComponentConditionSystem : EntityConditionSystem<MetaDataComponent, HasComponentCondition>
{
    protected override void Condition(Entity<MetaDataComponent> entity, ref EntityConditionEvent<HasComponentCondition> args)
    {
        foreach (var component in args.Condition.Components.Values)
        {
            if (!HasComp(entity, component.Component.GetType()))
                return;
        }

        args.Result = true;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class HasComponentCondition : EntityConditionBase<HasComponentCondition>
{
    /// <summary>
    /// The components the entity must have.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = default!;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => string.Empty;
}
