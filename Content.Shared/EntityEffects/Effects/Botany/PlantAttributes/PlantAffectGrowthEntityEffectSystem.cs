using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
/// Entity effect that increments plant age / growth cycle.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantAffectGrowthEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantAffectGrowth>
{
    [Dependency] private readonly PlantHarvestSystem _plantHarvest = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantAffectGrowth> args)
    {
        if (_plantHolder.IsDead(entity.Owner))
            return;

        _plantHarvest.AffectGrowth(entity.Owner, (int)args.Effect.Amount);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantAffectGrowth : BasePlantAdjustAttribute<PlantAffectGrowth>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-growth";
}
