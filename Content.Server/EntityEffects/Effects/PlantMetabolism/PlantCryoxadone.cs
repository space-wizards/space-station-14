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
        if (!args.EntityManager.TryGetComponent(args.TargetEntity, out PlantComponent? plantComp)
            || plantComp.Dead || plantComp.Seed == null || plantComp.Seed.Immutable)
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
        plantSys.Update(args.TargetEntity, plantComp);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-plant-cryoxadone", ("chance", Probability));
}
