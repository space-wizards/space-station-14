using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    public sealed partial class PlantAdjustMutationLevel : PlantAdjustAttribute
    {
        public override string GuidebookAttributeName { get; set; } = "plant-attribute-mutation-level";


        public override void Effect(ReagentEffectArgs args)
        {
            if (!CanMetabolize(args.SolutionEntity, out var plantHolderComp, args.EntityManager))
                return;

            plantHolderComp.MutationLevel += Amount * plantHolderComp.MutationMod;
        }
    }
}
