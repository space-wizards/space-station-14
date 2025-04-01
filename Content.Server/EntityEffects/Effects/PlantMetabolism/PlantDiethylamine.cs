using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
[DataDefinition]
public sealed partial class PlantDiethylamine : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!args.EntityManager.TryGetComponent(args.TargetEntity, out PlantHolderComponent? plantHolderComp)
                                || plantHolderComp.Seed == null || plantHolderComp.Dead ||
                                plantHolderComp.Seed.Immutable)
            return;


        var plantHolder = args.EntityManager.System<PlantHolderSystem>();

        var random = IoCManager.Resolve<IRobustRandom>();

        if (random.Prob(0.1f))
        {
            plantHolder.EnsureUniqueSeed(args.TargetEntity, plantHolderComp);
            plantHolderComp.Seed.Lifespan++;
        }

        if (random.Prob(0.1f))
        {
            plantHolder.EnsureUniqueSeed(args.TargetEntity, plantHolderComp);
            plantHolderComp.Seed.Endurance++;
        }
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-plant-diethylamine", ("chance", Probability));
}

