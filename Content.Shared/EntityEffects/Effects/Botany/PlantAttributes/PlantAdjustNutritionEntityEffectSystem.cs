using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the nutrition of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustNutritionEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantAdjustNutrition>
{
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantAdjustNutrition> args)
    {
        _plantTray.AdjustNutrient(entity.AsNullable(), args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustNutrition : BasePlantAdjustAttribute<PlantAdjustNutrition>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-nutrition";
}
