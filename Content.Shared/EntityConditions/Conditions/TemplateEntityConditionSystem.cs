using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;
///<summary>
/// A basic summary of this condition.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class TemplateEntityConditionSystem : EntityConditionSystem<MetaDataComponent, Template>
{
    protected override void Condition(Entity<MetaDataComponent> entity, ref EntityConditionEvent<Template> args)
    {
        // Condition goes here.
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class Template : EntityConditionBase<Template>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
