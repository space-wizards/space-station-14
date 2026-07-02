using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// A brief summary of the effect.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class TemplateEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Template>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Template> args)
    {
        // Effect goes here.
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Template : EntityEffectBase<Template>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null;
}
