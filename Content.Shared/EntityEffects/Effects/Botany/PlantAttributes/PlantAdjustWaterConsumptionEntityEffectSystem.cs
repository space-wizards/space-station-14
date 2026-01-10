using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the water consumption of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustWaterConsumptionEntityEffectSystem : EntityEffectSystem<BasicGrowthComponent, PlantAdjustWaterConsumption>
{
    [Dependency] private readonly BasicGrowthSystem _basicGrowth = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<BasicGrowthComponent> entity, ref EntityEffectEvent<PlantAdjustWaterConsumption> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _basicGrowth.AdjustWaterConsumption(entity.AsNullable(), args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustWaterConsumption : BasePlantAdjustAttribute<PlantAdjustWaterConsumption>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-water-consumption";
    public override bool GuidebookIsAttributePositive { get; protected set; } = false;
}
