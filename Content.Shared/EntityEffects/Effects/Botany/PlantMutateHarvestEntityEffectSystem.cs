using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.Botany;

/// <summary>
/// Plant mutation entity effect that changes repeatability of plant harvesting (without re-planting).
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateHarvestEntityEffectSystem : EntityEffectSystem<PlantComponent, PlantMutateHarvest>
{
    [Dependency] private readonly PlantHarvestSystem _plantHarvest = default!;

    protected override void Effect(Entity<PlantComponent> entity, ref EntityEffectEvent<PlantMutateHarvest> args)
    {
        _plantHarvest.ChangeHarvestRepeat(entity.Owner);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PlantMutateHarvest : EntityEffectBase<PlantMutateHarvest>;
