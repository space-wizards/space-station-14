using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the water of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustWaterEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantAdjustWater>
{
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantAdjustWater> args)
    {
        _plantTray.AdjustWater(entity.AsNullable(), args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustWater : BasePlantAdjustAttribute<PlantAdjustWater>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-water";
}
