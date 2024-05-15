using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    [UsedImplicitly]
    public sealed partial class PlantAdjustToxins : PlantAdjustAttribute
    {
        public PlantAdjustToxins()
        {
            Attribute = "plant-attribute-toxins";
            Positive = false;
        }

        public override void Effect(ReagentEffectArgs args)
        {
            if (!CanMetabolize(args.SolutionEntity, out var plantHolderComp, args.EntityManager))
                return;

            plantHolderComp.Toxins += Amount;
        }
    }
}
