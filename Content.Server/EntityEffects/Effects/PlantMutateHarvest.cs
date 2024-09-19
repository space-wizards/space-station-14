using Content.Server.Botany;
using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
///     Upgrades a plant's harvest type.
/// </summary>
public sealed partial class PlantMutateHarvest : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var plant = args.EntityManager.GetComponent<PlantComponent>(args.TargetEntity);

        if (plant.Seed == null)
            return;

        if (plant.Seed.HarvestRepeat == HarvestType.NoRepeat)
            plant.Seed.HarvestRepeat = HarvestType.Repeat;
        else if (plant.Seed.HarvestRepeat == HarvestType.Repeat)
            plant.Seed.HarvestRepeat = HarvestType.SelfHarvest;
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}
