using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts a plant's maturation time.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustMaturationEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantAdjustMaturation>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantSystem _plant = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantAdjustMaturation> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _plant.AdjustMaturation(entity.AsNullable(), args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustMaturation : BasePlantAdjustAttribute<PlantAdjustMaturation>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-maturation";
    public override bool GuidebookIsAttributePositive { get; protected set; } = false;
}
