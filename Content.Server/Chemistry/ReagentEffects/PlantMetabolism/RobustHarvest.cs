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
        ///<summary>
        /// The maximum value of the potency bonus the plant can achieve with the reagent.
        ///</summary>
        [DataField]
        public int PotencyLimit = 45;

        ///<summary>
        /// Increase of potency bonus per metabolism step.
        ///</summary>
        [DataField]
        public int PotencyIncrease = 3;

        ///<summary>
        /// The probability per metabolism step that the current plant's Yield will be reduced if the potency bonus is already maxed out.
        ///</summary>
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
            else if (plantHolderComp.Seed.Yield * plantHolderComp.YieldMod > 1f && random.Prob(YieldReductionProbability))
            {
                // Too much of a good thing reduces yield
                plantHolder.EnsureUniqueSeed(args.SolutionEntity, plantHolderComp);
                if (plantHolderComp.Seed.Yield != 0)
                {
                    plantHolderComp.YieldMod -= 1f / (float)plantHolderComp.Seed.Yield; //Reduces the yield of the current plant by one by changing YieldMod.
                }
            }
        }

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-missing", ("chance", Probability));
    }
}
