using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
[DataDefinition]
public sealed partial class PlantCryoxadone : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantHolderComp = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);
        if (plantHolderComp.PlantUid == null)
            return;
        var plantComp = args.EntityManager.GetComponent<PlantComponent>(plantHolderComp.PlantUid.Value);
        if (plantComp == null || plantComp.Dead || plantComp.Seed == null)
            return;

        var deviation = 0;
        var seed = plantComp.Seed;
        var random = IoCManager.Resolve<IRobustRandom>();
        if (plantComp.Age > seed.Maturation)
            deviation = (int)Math.Max(seed.Maturation - 1, plantComp.Age - random.Next(7, 10));
        else
            deviation = (int)(seed.Maturation / seed.GrowthStages);
        plantComp.Age -= deviation;
        plantComp.SkipAging++;
        var plantSys = args.EntityManager.System<PlantSystem>();
        plantSys.Update(plantHolderComp.PlantUid.Value, plantComp);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-plant-cryoxadone", ("chance", Probability));
}
