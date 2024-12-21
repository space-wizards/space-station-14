using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
[DataDefinition]
public sealed partial class RobustHarvest : EntityEffect
{
    [DataField]
    public int PotencyLimit = 50;

    [DataField]
    public int PotencyIncrease = 3;

    [DataField]
    public int PotencySeedlessThreshold = 30;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantHolderComp = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);
        if (plantHolderComp.PlantUid == null)
            return;
        var plantComp = args.EntityManager.GetComponent<PlantComponent>(plantHolderComp.PlantUid.Value);
        if (plantComp == null || plantComp.Dead || plantComp.Seed == null || plantComp.Seed.Immutable)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();

        if (plantComp.Seed.Potency < PotencyLimit)
        {
            plantComp.Seed.Potency = Math.Min(plantComp.Seed.Potency + PotencyIncrease, PotencyLimit);

            if (plantComp.Seed.Potency > PotencySeedlessThreshold)
            {
                plantComp.Seed.Seedless = true;
            }
        }
        else if (plantComp.Seed.Yield > 1 && random.Prob(0.1f))
        {
            // Too much of a good thing reduces yield
            plantComp.Seed.Yield--;
        }
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-plant-robust-harvest", ("seedlesstreshold", PotencySeedlessThreshold), ("limit", PotencyLimit), ("increase", PotencyIncrease), ("chance", Probability));
}
