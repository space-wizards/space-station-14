using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the pest tolerance of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustPestToleranceEntityEffectSystem : EntityEffectSystem<WeedPestGrowthComponent, PlantAdjustPestTolerance>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly WeedPestGrowthSystem _weedPestGrowth = default!;

    protected override void Effect(Entity<WeedPestGrowthComponent> entity, ref EntityEffectEvent<PlantAdjustPestTolerance> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _weedPestGrowth.AdjustPestTolerance(entity.AsNullable(), args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustPestTolerance : BasePlantAdjustAttribute<PlantAdjustPestTolerance>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-pest-tolerance";
}
