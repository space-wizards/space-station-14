using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany;

namespace Content.Server.EntityEffects.Effects.Botany;

/// <summary>
/// Plant mutation entity effect that changes repeatability of plant harvesting (without re-planting).
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class PlantMutateHarvestEntityEffectSystem : EntityEffectSystem<PlantTrayComponent, PlantMutateHarvest>
{
    protected override void Effect(Entity<PlantTrayComponent> entity, ref EntityEffectEvent<PlantMutateHarvest> args)
    {
        if (entity.Comp.PlantEntity == null || Deleted(entity.Comp.PlantEntity))
            return;

        var harvest = EnsureComp<PlantHarvestComponent>(entity.Comp.PlantEntity.Value);
        switch (harvest.HarvestRepeat)
        {
            case HarvestType.NoRepeat:
                harvest.HarvestRepeat = HarvestType.Repeat;
                break;
            case HarvestType.Repeat:
                harvest.HarvestRepeat = HarvestType.SelfHarvest;
                break;
        }
    }
}
