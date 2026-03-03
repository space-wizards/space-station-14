using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts a plant's yield.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustYieldEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantAdjustYield>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantSystem _plant = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantAdjustYield> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _plant.AdjustYield(entity.AsNullable(), (int)args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustYield : BasePlantAdjustAttribute<PlantAdjustYield>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-yield";
}
