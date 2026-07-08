using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the toxin tolerance of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustToxinsToleranceEntityEffectSystem : EntityEffectSystem<PlantToxinsComponent, PlantAdjustToxinsTolerance>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantToxinsSystem _plantToxins = default!;

    protected override void Effect(Entity<PlantToxinsComponent> entity, ref EntityEffectEvent<PlantAdjustToxinsTolerance> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _plantToxins.AdjustToxinsTolerance(entity.AsNullable(), args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustToxinsTolerance : BasePlantAdjustAttribute<PlantAdjustToxinsTolerance>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-toxins-tolerance";
}
