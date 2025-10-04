using Content.Server.Botany;
using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany;

namespace Content.Server.EntityEffects.Effects.Botany;

public sealed partial class PlantMutateHarvestEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantMutateHarvest>
{
    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantMutateHarvest> args)
    {
        if (entity.Comp.Seed == null)
            return;

        switch (entity.Comp.Seed.HarvestRepeat)
        {
            case HarvestType.NoRepeat:
                entity.Comp.Seed.HarvestRepeat = HarvestType.Repeat;
                break;
            case HarvestType.Repeat:
                entity.Comp.Seed.HarvestRepeat = HarvestType.SelfHarvest;
                break;
        }
    }
}
