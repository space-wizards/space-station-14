using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the pest tolerance of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustPestToleranceEntityEffectSystem : EntityEffectSystem<PlantWeedPestComponent, PlantAdjustPestTolerance>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantWeedPestSystem _plantWeedPest = default!;

    protected override void Effect(Entity<PlantWeedPestComponent> entity, ref EntityEffectEvent<PlantAdjustPestTolerance> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _plantWeedPest.AdjustPestTolerance(entity.AsNullable(), args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustPestTolerance : BasePlantAdjustAttribute<PlantAdjustPestTolerance>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-pest-tolerance";
}
