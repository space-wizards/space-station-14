using Content.Server.Botany.Components;
using Content.Shared.Botany;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    [UsedImplicitly]
    [DataDefinition]
    public class PlantDiethylamine : ReagentEffect
    {
        public override void Metabolize(ReagentEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out PlantHolderComponent? plantHolderComp)
                                    || plantHolderComp.Seed == null || plantHolderComp.Dead ||
                                    plantHolderComp.Seed.Immutable)
                return;

            var random = IoCManager.Resolve<IRobustRandom>();

            var chance = MathHelper.Lerp(15f, 125f, plantHolderComp.Seed.Lifespan) * 2f;
            if (random.Prob(chance))
            {
                plantHolderComp.CheckForDivergence(true);
                plantHolderComp.Seed.Lifespan++;
            }

            chance = MathHelper.Lerp(15f, 125f, plantHolderComp.Seed.Endurance) * 2f;
            if (random.Prob(chance))
            {
                plantHolderComp.CheckForDivergence(true);
                plantHolderComp.Seed.Endurance++;
            }
        }
    }
}
