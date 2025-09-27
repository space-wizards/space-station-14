using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;

public sealed partial class TemplateEntityConditionSystem : EntityConditionSystem<MetaDataComponent, Template>
{
    protected override void Condition(Entity<MetaDataComponent> entity, ref EntityConditionEvent<Template> args)
    {
        // Condition goes here.
    }
}

public sealed partial class Template : EntityConditionBase<Template>
{
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
