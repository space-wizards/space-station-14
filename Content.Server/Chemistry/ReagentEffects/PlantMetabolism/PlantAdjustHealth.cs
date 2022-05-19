using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    public sealed class PlantAdjustHealth : PlantAdjustAttribute
    {
        public override void Effect(ReagentEffectArgs args)
        {
            if (!CanMetabolize(args.SolutionEntity, out var plantHolderComp, args.EntityManager))
                return;

            plantHolderComp.Health += Amount;
            plantHolderComp.CheckHealth();
        }
    }
}
