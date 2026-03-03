using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that adjusts the weed tolerance of a plant.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAdjustWeedToleranceEntityEffectSystem : EntityEffectSystem<WeedPestGrowthComponent, PlantAdjustWeedTolerance>
{
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly WeedPestGrowthSystem _weedPestGrowth = default!;

    protected override void Effect(Entity<WeedPestGrowthComponent> entity, ref EntityEffectEvent<PlantAdjustWeedTolerance> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _weedPestGrowth.AdjustWeedTolerance(entity.AsNullable(), args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAdjustWeedTolerance : BasePlantAdjustAttribute<PlantAdjustWeedTolerance>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-weed-tolerance";
}
