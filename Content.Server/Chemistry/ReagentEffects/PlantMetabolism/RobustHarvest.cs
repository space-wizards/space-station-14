using Content.Server.Botany.Components;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class RobustHarvest : ReagentEffect
    {
        public override void Effect(ReagentEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out PlantHolderComponent? plantHolderComp)
                                    || plantHolderComp.Seed == null || plantHolderComp.Dead ||
                                    plantHolderComp.Seed.Immutable)
                return;

            const int potencyLimit = 50;
            var random = IoCManager.Resolve<IRobustRandom>();

            if (plantHolderComp.Seed.Potency < potencyLimit)
            {
                plantHolderComp.EnsureUniqueSeed();
                plantHolderComp.Seed.Potency = Math.Min(plantHolderComp.Seed.Potency + 3, potencyLimit);

                if (plantHolderComp.Seed.Potency > 30)
                {
                    plantHolderComp.Seed.Seedless = true;
                }
            }
            else if (plantHolderComp.Seed.Yield > 1 && random.Prob(0.1f))
            {
                // Too much of a good thing reduces yield
                plantHolderComp.EnsureUniqueSeed();
                plantHolderComp.Seed.Yield--;
            }
        }
    }
}
