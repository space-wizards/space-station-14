using Content.Server.Botany.Systems;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    public sealed partial class PlantAdjustHealth : PlantAdjustAttribute
    {
        public override void Effect(ReagentEffectArgs args)
        {
            if (!CanMetabolize(args.SolutionEntity, out var plantHolderComp, args.EntityManager))
                return;

            var plantHolder = args.EntityManager.System<PlantHolderSystem>();

            plantHolderComp.Health += Amount;
            plantHolder.CheckHealth(args.SolutionEntity, plantHolderComp);
        }
    }
}
