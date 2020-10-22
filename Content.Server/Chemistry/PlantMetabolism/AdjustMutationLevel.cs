#nullable enable
using Content.Server.GameObjects.Components.Botany;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Chemistry.PlantMetabolism
{
    [UsedImplicitly]
    public class AdjustMutationLevel : AdjustAttribute
    {
        public override void Metabolize(IEntity plantHolder, float customPlantMetabolism = 1f)
        {
            if (plantHolder.Deleted || !plantHolder.TryGetComponent(out PlantHolderComponent? plantHolderComp)
                                    || !CanMetabolize())
                return;

            plantHolderComp.MutationLevel += Amount * plantHolderComp.MutationMod * customPlantMetabolism;
        }
    }
}
