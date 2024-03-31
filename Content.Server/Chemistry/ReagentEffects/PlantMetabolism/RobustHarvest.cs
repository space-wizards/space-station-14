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
        public int PotencySeedlessThreshold = 30;

        public override void Effect(ReagentEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out PlantHolderComponent? plantHolderComp)
                                    || plantHolderComp.Seed == null || plantHolderComp.Dead ||
                                    plantHolderComp.Seed.Immutable)
                return;


            var plantHolder = args.EntityManager.System<PlantHolderSystem>();
            var random = IoCManager.Resolve<IRobustRandom>();

            if (plantHolderComp.Seed.Potency < PotencyLimit)
            {
                plantHolder.EnsureUniqueSeed(args.SolutionEntity, plantHolderComp);
                plantHolderComp.Seed.Potency = Math.Min(plantHolderComp.Seed.Potency + PotencyIncrease, PotencyLimit);

                if (plantHolderComp.Seed.Potency > PotencySeedlessThreshold)
                {
                    plantHolderComp.Seed.Seedless = true;
                }
            }
            else if (plantHolderComp.Seed.Yield > 1 && random.Prob(0.1f))
            {
                // Too much of a good thing reduces yield
                plantHolder.EnsureUniqueSeed(args.SolutionEntity, plantHolderComp);
                plantHolderComp.Seed.Yield--;
            }
        }

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-missing", ("chance", Probability));
    }
}
