using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;
///<summary>
/// A basic summary of this condition.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class TemplateEntityConditionSystem : EntityConditionSystem<MetaDataComponent, TemplateCondition>
{
    protected override void Condition(Entity<MetaDataComponent> entity,
        TemplateCondition condition,
        EntityUid? sourceEnt,
        ref bool result)
    {
        // Condition goes here.
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class TemplateCondition : EntityCondition
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
