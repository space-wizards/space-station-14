using Content.Shared.EntityEffects;

namespace Content.Shared.EntityConditions.Conditions;

public sealed partial class TemplateEntityConditionSystem : EntityConditionSystem<MetaDataComponent, Template>
{
    protected override void Condition(Entity<MetaDataComponent> entity, ref EntityConditionEvent<Template> args)
    {
        // Condition goes here.
    }
}

public sealed class Template : EntityConditionBase<Template>
{
    // Datafields go here.
}
