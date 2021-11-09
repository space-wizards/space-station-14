using System;
using Content.Server.Botany.Components;
using Content.Shared.Botany;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    [UsedImplicitly]
    [DataDefinition]
    public class PlantClonexadone : ReagentEffect
    {
        public override void Metabolize(EntityUid plantHolder, EntityUid organEntity, Solution.ReagentQuantity reagent, IEntityManager entityManager)
        {
            if (!entityManager.TryGetComponent(plantHolder, out PlantHolderComponent? plantHolderComp)
            || plantHolderComp.Seed == null || plantHolderComp.Dead)
                return;

            var deviation = 0;
            var seed = plantHolderComp.Seed;
            var random = IoCManager.Resolve<IRobustRandom>();
            if (plantHolderComp.Age > seed.Maturation)
                deviation = (int) Math.Max(seed.Maturation - 1, plantHolderComp.Age - random.Next(7, 10));
            else
                deviation = (int) (seed.Maturation / seed.GrowthStages);
            plantHolderComp.Age -= deviation;
            plantHolderComp.SkipAging++;
            plantHolderComp.ForceUpdate = true;
        }
    }
}
