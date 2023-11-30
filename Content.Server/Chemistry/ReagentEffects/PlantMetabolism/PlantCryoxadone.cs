using Content.Server.Botany.Components;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class PlantCryoxadone : ReagentEffect
    {
        public override void Effect(ReagentEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out PlantHolderComponent? plantHolderComp)
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

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-missing", ("chance", Probability));
    }
}
