using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the nutrient consumption of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustNutrientConsumptionEntityEffectSystem : EntityEffectSystem<PlantGrowthComponent, PlantAdjustNutrientConsumption>
{
    [Dependency] private readonly PlantGrowthSystem _plantGrowth = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantGrowthComponent> entity, ref EntityEffectEvent<PlantAdjustNutrientConsumption> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _plantGrowth.AdjustNutrientConsumption(entity.AsNullable(), args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustNutrientConsumption : BasePlantAdjustAttribute<PlantAdjustNutrientConsumption>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-nutrient-consumption";
    public override bool GuidebookIsAttributePositive { get; protected set; } = false;
}
