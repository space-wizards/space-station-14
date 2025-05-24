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
        var plantholder = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);

        if (plantholder.Seed == null)
            return;

        if (plantholder.Seed.HarvestRepeat == HarvestType.NoRepeat)
            plantholder.Seed.HarvestRepeat = HarvestType.Repeat;
        else if (plantholder.Seed.HarvestRepeat == HarvestType.Repeat)
            plantholder.Seed.HarvestRepeat = HarvestType.SelfHarvest;
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}
