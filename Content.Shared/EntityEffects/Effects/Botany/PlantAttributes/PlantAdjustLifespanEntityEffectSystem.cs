using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts a plant's lifespan.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustLifespanEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantAdjustLifespan>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantSystem _plant = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantAdjustLifespan> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _plant.AdjustLifespan(entity.AsNullable(), (int)args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustLifespan : BasePlantAdjustAttribute<PlantAdjustLifespan>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-lifespan";
}
