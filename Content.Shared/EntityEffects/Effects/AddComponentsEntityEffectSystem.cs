using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Adds a set of components to this entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class AddComponentsEntityEffectSystem : EntityEffectSystem<MetaDataComponent, AddComponents>
{
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<AddComponents> args)
    {
        EntityManager.AddComponents(entity, args.Effect.Components, args.Effect.ReplaceExisting);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AddComponents : EntityEffectBase<AddComponents>
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    [DataField]
    public bool ReplaceExisting;
}
