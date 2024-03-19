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
    public sealed partial class RobustHarvest : ReagentEffect
    {
        [DataField]
        public int PotencyLimit = 45;

        [DataField]
        public int PotencyIncrease = 3;

        [DataField]
        public float YieldReductionProbability = 0.1f;



        public override void Effect(ReagentEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out PlantHolderComponent? plantHolderComp)
                                    || plantHolderComp.Seed == null || plantHolderComp.Dead ||
                                    plantHolderComp.Seed.Immutable)
                return;


            var plantHolder = args.EntityManager.System<PlantHolderSystem>();
            var random = IoCManager.Resolve<IRobustRandom>();

            if (plantHolderComp.PotencyBonus < PotencyLimit)
            {
                plantHolder.EnsureUniqueSeed(args.SolutionEntity, plantHolderComp);
                plantHolderComp.PotencyBonus = Math.Min(plantHolderComp.PotencyBonus + PotencyIncrease, PotencyLimit);
            }
            else if ((float) plantHolderComp.Seed.Yield * plantHolderComp.YieldMod > 1f && random.Prob(YieldReductionProbability))
            {
                // Too much of a good thing reduces yield
                plantHolder.EnsureUniqueSeed(args.SolutionEntity, plantHolderComp);
                plantHolderComp.YieldMod -= 1f / (float)plantHolderComp.Seed.Yield;
            }
        }

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-missing", ("chance", Probability));
    }
}
