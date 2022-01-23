using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    public class PlantAdjustMutationLevel : PlantAdjustAttribute
    {
        public override void Effect(ReagentEffectArgs args)
        {
            if (!CanMetabolize(args.SolutionEntity, out var plantHolderComp, args.EntityManager))
                return;

            plantHolderComp.MutationLevel += Amount * plantHolderComp.MutationMod;
        }
    }
}
