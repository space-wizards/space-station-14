using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany;

namespace Content.Server.EntityEffects.Effects.Botany;

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
