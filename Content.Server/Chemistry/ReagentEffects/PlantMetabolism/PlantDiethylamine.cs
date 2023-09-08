using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class PlantDiethylamine : ReagentEffect
    {
        public override void Effect(ReagentEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out PlantHolderComponent? plantHolderComp)
                                    || plantHolderComp.Seed == null || plantHolderComp.Dead ||
                                    plantHolderComp.Seed.Immutable)
                return;


            var plantHolder = args.EntityManager.System<PlantHolderSystem>();

            var random = IoCManager.Resolve<IRobustRandom>();

            if (random.Prob(0.1f))
            {
                plantHolder.EnsureUniqueSeed(args.SolutionEntity, plantHolderComp);
                plantHolderComp.Seed.Lifespan++;
            }

            if (random.Prob(0.1f))
            {
                plantHolder.EnsureUniqueSeed(args.SolutionEntity, plantHolderComp);
                plantHolderComp.Seed.Endurance++;
            }
        }

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-missing", ("chance", Probability));
    }
}
