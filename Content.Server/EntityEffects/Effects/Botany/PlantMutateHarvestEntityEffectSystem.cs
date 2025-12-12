using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany;

namespace Content.Server.EntityEffects.Effects.Botany;

/// <summary>
/// Plant mutation entity effect that changes repeatability of plant harvesting (without re-planting).
/// </summary>
public sealed partial class PlantMutateHarvestEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantMutateHarvest>
{
    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantMutateHarvest> args)
    {
        if (entity.Comp.Seed == null)
            return;

        var harvest = EnsureComp<PlantHarvestComponent>(entity);
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
