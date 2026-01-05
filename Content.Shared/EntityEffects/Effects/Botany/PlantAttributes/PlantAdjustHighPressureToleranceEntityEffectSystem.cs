using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the maximum pressure tolerance of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustHighPressureToleranceEntityEffectSystem : EntityEffectSystem<AtmosphericGrowthComponent, PlantAdjustHighPressureTolerance>
{
    [Dependency] private readonly SharedAtmosphericGrowthSystem _atmosphericGrowth = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<AtmosphericGrowthComponent> entity, ref EntityEffectEvent<PlantAdjustHighPressureTolerance> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _atmosphericGrowth.AdjustHighPressureTolerance(entity.AsNullable(), args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustHighPressureTolerance : BasePlantAdjustAttribute<PlantAdjustHighPressureTolerance>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-high-pressure-tolerance";
}
