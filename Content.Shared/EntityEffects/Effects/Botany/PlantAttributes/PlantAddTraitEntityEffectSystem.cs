using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adds a <see cref="PlantTraitPrototype"/> to a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAddTraitEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantAddTrait>
{
    [Dependency] private PlantHolderSystem _plantHolder = default!;
    [Dependency] private PlantMutationSystem _mutation = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantAddTrait> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _mutation.AddTrait(entity.AsNullable(), args.Effect.Trait);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAddTrait : EntityEffectBase<PlantAddTrait>
{
    /// <summary>
    /// The trait to add to the plant.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PlantTraitPrototype> Trait;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (!prototype.TryIndex(Trait, out var trait))
            return null;

        return Loc.GetString("entity-effect-guidebook-plant-change-trait",
            ("change", "Add"),
            ("chance", Probability),
            ("trait", Loc.GetString(trait.Name)));
    }
}
