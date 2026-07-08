using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the maximum temperature tolerance of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustHighHeatToleranceEntityEffectSystem : EntityEffectSystem<PlantAtmosphericComponent, PlantAdjustHighHeatTolerance>
{
    [Dependency] private readonly SharedPlantAtmosphericSystem _plantAtmospheric = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantAtmosphericComponent> entity, ref EntityEffectEvent<PlantAdjustHighHeatTolerance> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _plantAtmospheric.AdjustHighHeatTolerance(entity.AsNullable(), args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustHighHeatTolerance : BasePlantAdjustAttribute<PlantAdjustHighHeatTolerance>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-high-heat-tolerance";
}
