using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    [UsedImplicitly]
    public sealed class PlantAdjustWater : PlantAdjustAttribute
    {
        public override void Effect(ReagentEffectArgs args)
        {
            if (!CanMetabolize(args.SolutionEntity, out var plantHolderComp, args.EntityManager, mustHaveAlivePlant: false))
                return;

            plantHolderComp.AdjustWater(Amount);
        }
    }
}
