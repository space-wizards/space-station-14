using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the minimum pressure tolerance of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustLowPressureToleranceEntityEffectSystem : EntityEffectSystem<PlantAtmosphericComponent, PlantAdjustLowPressureTolerance>
{
    [Dependency] private readonly SharedPlantAtmosphericSystem _plantAtmospheric = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantAtmosphericComponent> entity, ref EntityEffectEvent<PlantAdjustLowPressureTolerance> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _plantAtmospheric.AdjustLowPressureTolerance(entity.AsNullable(), args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustLowPressureTolerance : BasePlantAdjustAttribute<PlantAdjustLowPressureTolerance>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-low-pressure-tolerance";
    public override bool GuidebookIsAttributePositive { get; protected set; } = false;
}
