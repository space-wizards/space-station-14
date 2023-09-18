using Content.Server.Botany.Systems;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    [UsedImplicitly]
    public sealed partial class PlantAffectGrowth : PlantAdjustAttribute
    {
        public override void Effect(ReagentEffectArgs args)
        {
            if (!CanMetabolize(args.SolutionEntity, out var plantHolderComp, args.EntityManager))
                return;

            var plantHolder = args.EntityManager.System<PlantHolderSystem>();

            plantHolder.AffectGrowth(args.SolutionEntity, (int) Amount, plantHolderComp);
        }
    }
}
