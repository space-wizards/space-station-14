using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the mutation level of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustMutationLevelEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantAdjustMutationLevel>
{
    [Dependency] private PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantAdjustMutationLevel> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _plantHolder.AdjustsMutationLevel(entity.Owner, args.Effect.MutationTable, args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustMutationLevel : BasePlantAdjustAttribute<PlantAdjustMutationLevel>
{
    /// <summary>
    /// The mutation table whose level is adjusted.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<RandomPlantMutationListPrototype> MutationTable;

    public override string GuidebookAttributeName { get; set; } = "plant-attribute-mutation-level";
}
