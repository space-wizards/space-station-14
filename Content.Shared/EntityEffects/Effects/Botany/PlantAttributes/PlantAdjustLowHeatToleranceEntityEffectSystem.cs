using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the minimum temperature tolerance of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustLowHeatToleranceEntityEffectSystem : EntityEffectSystem<PlantAtmosphericComponent, PlantAdjustLowHeatTolerance>
{
    [Dependency] private readonly SharedPlantAtmosphericSystem _plantAtmospheric = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantAtmosphericComponent> entity, ref EntityEffectEvent<PlantAdjustLowHeatTolerance> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _plantAtmospheric.AdjustLowHeatTolerance(entity.AsNullable(), args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustLowHeatTolerance : BasePlantAdjustAttribute<PlantAdjustLowHeatTolerance>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-low-heat-tolerance";
    public override bool GuidebookIsAttributePositive { get; protected set; } = false;
}
