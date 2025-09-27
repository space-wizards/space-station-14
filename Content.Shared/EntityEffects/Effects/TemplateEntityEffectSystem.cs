namespace Content.Shared.EntityEffects.Effects;

public sealed partial class TemplateEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Template>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Template> args)
    {
        // Effect goes here.
    }
}

public sealed partial class Template : EntityEffectBase<Template>
{
    // Datafields go here.
}
