using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adds or removes a plant trait.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantChangeTraitsEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantChangeTraits>
{
    [Dependency] private readonly PlantTraitsSystem _plantTraits = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantChangeTraits> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        var trait = args.Effect.Trait;
        if (args.Effect.Remove)
            _plantTraits.DelTrait(entity.Owner, trait);
        else
            _plantTraits.AddTrait(entity.Owner, trait);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantChangeTraits : EntityEffectBase<PlantChangeTraits>
{
    /// <summary>
    /// Name of a <see cref="PlantTrait"/> type.
    /// </summary>
    [DataField(required: true)]
    public PlantTrait Trait;

    /// <summary>
    /// If true, the trait is removed. If false, the trait is added.
    /// </summary>
    [DataField]
    public bool Remove;
}

