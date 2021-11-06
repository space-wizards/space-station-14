using Content.Shared.Chemistry.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    [UsedImplicitly]
    public class PlantAdjustWeeds : PlantAdjustAttribute
    {
        public override void Metabolize(IEntity plantHolder, IEntity organEntity, Solution.ReagentQuantity amount)
        {
            if (!CanMetabolize(plantHolder, out var plantHolderComp, false))
                return;

            plantHolderComp.WeedLevel += Amount;
        }
    }
}
