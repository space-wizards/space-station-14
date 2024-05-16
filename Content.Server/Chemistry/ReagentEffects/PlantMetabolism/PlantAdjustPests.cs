using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    [UsedImplicitly]
    public sealed partial class PlantAdjustPests : PlantAdjustAttribute
    {
        public override string GuidebookAttributeName { get; set; } = "plant-attribute-pests";
        public override bool GuidebookIsAttributePositive { get; protected set; } = false;

        public override void Effect(ReagentEffectArgs args)
        {
            if (!CanMetabolize(args.SolutionEntity, out var plantHolderComp, args.EntityManager))
                return;

            plantHolderComp.PestLevel += Amount;
        }
    }
}
