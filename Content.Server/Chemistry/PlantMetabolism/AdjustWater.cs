#nullable enable
using Content.Server.GameObjects.Components.Botany;
using Content.Shared.Interfaces.Chemistry;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Chemistry.PlantMetabolism
{
    [UsedImplicitly]
    public class AdjustWater : AdjustAttribute
    {
        public override void Metabolize(IEntity plantHolder, float customPlantMetabolism = 1f)
        {
            if (plantHolder.Deleted || !plantHolder.TryGetComponent(out PlantHolderComponent? plantHolderComp)
            || !CanMetabolize())
                return;

            plantHolderComp.AdjustWater(Amount);
        }
    }
}
