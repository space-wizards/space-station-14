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
        public int PotencyLimit = 50;

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

            if (plantHolderComp.CurrentPotency < PotencyLimit)
            {
                plantHolder.EnsureUniqueSeed(args.SolutionEntity, plantHolderComp);
                plantHolderComp.CurrentPotency = Math.Min(plantHolderComp.CurrentPotency + PotencyIncrease, PotencyLimit);
            }
            else if (plantHolderComp.Seed.Yield > 1 && random.Prob(YieldReductionProbability))
            {
                // Too much of a good thing reduces yield
                plantHolder.EnsureUniqueSeed(args.SolutionEntity, plantHolderComp);
                plantHolderComp.Seed.Yield--;
            }
        }

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-missing", ("chance", Probability));
    }
}
