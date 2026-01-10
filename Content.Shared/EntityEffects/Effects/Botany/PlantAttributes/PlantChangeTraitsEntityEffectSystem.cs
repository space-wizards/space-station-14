using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adds or removes a plant trait.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantChangeTraitsEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantChangeTraits>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantChangeTraits> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        var traitType = _componentFactory.GetComponent(args.Effect.Trait);
        if (traitType is not PlantTraitsComponent)
        {
            Log.Error(
                $"Component '{traitType}' (type: {traitType.GetType().Name}) is not a descendant of {nameof(PlantTraitsComponent)}.");
            return;
        }

        if (args.Effect.Remove)
            RemCompDeferred(entity.Owner, traitType);
        else if (!HasComp(entity.Owner, traitType.GetType()))
            AddComp(entity.Owner, traitType);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantChangeTraits : EntityEffectBase<PlantChangeTraits>
{
    /// <summary>
    /// Name of a <see cref="PlantTraitsComponent"/> type.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(ComponentNameSerializer))]
    public string Trait;

    /// <summary>
    /// If true, the trait is removed. If false, the trait is added.
    /// </summary>
    [DataField]
    public bool Remove;
}

