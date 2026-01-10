using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts a plant's production time.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustProductionEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantAdjustProduction>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantSystem _plant = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantAdjustProduction> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _plant.AdjustProduction(entity.AsNullable(), args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustProduction : BasePlantAdjustAttribute<PlantAdjustProduction>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-production";
    public override bool GuidebookIsAttributePositive { get; protected set; } = false;
}
