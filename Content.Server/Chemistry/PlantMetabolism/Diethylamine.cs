#nullable enable
using Content.Server.GameObjects.Components.Botany;
using Content.Shared.Interfaces.Chemistry;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.Chemistry.PlantMetabolism
{
    [UsedImplicitly]
    public class Diethylamine : IPlantMetabolizable
    {
        public void ExposeData(ObjectSerializer serializer)
        {
        }

        public void Metabolize(IEntity plantHolder, float customPlantMetabolism = 1)
        {
            if (plantHolder.Deleted || !plantHolder.TryGetComponent(out PlantHolderComponent? plantHolderComp)
                                    || plantHolderComp.Seed == null || plantHolderComp.Dead ||
                                    plantHolderComp.Seed.Immutable)
                return;

            var random = IoCManager.Resolve<IRobustRandom>();

            var chance = MathHelper.Lerp(15f, 125f, plantHolderComp.Seed.Lifespan) * 2f * customPlantMetabolism;
            if (random.Prob(chance))
            {
                plantHolderComp.CheckForDivergence(true);
                plantHolderComp.Seed.Lifespan++;
            }

            chance = MathHelper.Lerp(15f, 125f, plantHolderComp.Seed.Endurance) * 2f * customPlantMetabolism;
            if (random.Prob(chance))
            {
                plantHolderComp.CheckForDivergence(true);
                plantHolderComp.Seed.Endurance++;
            }
        }
    }
}
