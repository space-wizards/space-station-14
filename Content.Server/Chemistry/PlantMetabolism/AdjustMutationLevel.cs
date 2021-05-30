#nullable enable
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Chemistry.PlantMetabolism
{
    [UsedImplicitly]
    public class AdjustMutationLevel : AdjustAttribute
    {
        public override void Metabolize(IEntity plantHolder, float customPlantMetabolism = 1f)
        {
            if (!CanMetabolize(plantHolder, out var plantHolderComp, false))
                return;

            plantHolderComp.MutationLevel += Amount * plantHolderComp.MutationMod * customPlantMetabolism;
        }
    }
}
