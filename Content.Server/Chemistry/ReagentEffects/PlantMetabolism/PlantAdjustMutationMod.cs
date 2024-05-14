using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    [UsedImplicitly]
    public sealed partial class PlantAdjustMutationMod : PlantAdjustAttribute
    {
        public PlantAdjustMutationMod()
        {
            Attribute = "plant-attribute-mutation-mod";
        }

        public override void Effect(ReagentEffectArgs args)
        {
            if (!CanMetabolize(args.SolutionEntity, out var plantHolderComp, args.EntityManager))
                return;

            plantHolderComp.MutationMod += Amount;
        }
    }
}
