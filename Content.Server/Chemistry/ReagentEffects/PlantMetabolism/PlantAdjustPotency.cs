using Content.Server.Botany.Systems;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    public sealed partial class PlantAdjustPotency : PlantAdjustAttribute
    {
        public override void Effect(ReagentEffectArgs args)
        {
            if (!CanMetabolize(args.SolutionEntity, out var plantHolderComp, args.EntityManager))
                return;

            if (plantHolderComp.Seed == null)
                return;

            var plantHolder = args.EntityManager.System<PlantHolderSystem>();

            plantHolderComp.Seed.Potency = Math.Max(plantHolderComp.Seed.Potency + Amount, 1);
        }
    }
}
