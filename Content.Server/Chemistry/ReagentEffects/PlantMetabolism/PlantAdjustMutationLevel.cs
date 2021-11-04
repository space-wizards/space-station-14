using Content.Shared.Chemistry.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    public class PlantAdjustMutationLevel : PlantAdjustAttribute
    {
        public override void Metabolize(IEntity plantHolder, Solution.ReagentQuantity amount)
        {
            if (!CanMetabolize(plantHolder, out var plantHolderComp, false))
                return;

            plantHolderComp.MutationLevel += Amount * plantHolderComp.MutationMod;
        }
    }
}
