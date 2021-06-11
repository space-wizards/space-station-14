#nullable enable
using Content.Server.Botany.Components;
using Content.Shared.Botany;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.PlantMetabolism
{
    [UsedImplicitly]
    [DataDefinition]
    public class RobustHarvest : IPlantMetabolizable
    {
        public void Metabolize(IEntity plantHolder, float customPlantMetabolism = 1f)
        {
            if (plantHolder.Deleted || !plantHolder.TryGetComponent(out PlantHolderComponent? plantHolderComp)
                                    || plantHolderComp.Seed == null || plantHolderComp.Dead ||
                                    plantHolderComp.Seed.Immutable)
                return;

            var random = IoCManager.Resolve<IRobustRandom>();

            var chance = MathHelper.Lerp(15f, 150f, plantHolderComp.Seed.Potency) * 3.5f * customPlantMetabolism;

            if (random.Prob(chance))
            {
                plantHolderComp.CheckForDivergence(true);
                plantHolderComp.Seed.Potency++;
            }

            chance = MathHelper.Lerp(6f, 2f, plantHolderComp.Seed.Yield) * 0.15f * customPlantMetabolism;

            if (random.Prob(chance))
            {
                plantHolderComp.CheckForDivergence(true);
                plantHolderComp.Seed.Yield--;
            }
        }
    }
}
