using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Nutrition.Components;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects
{
    [UsedImplicitly]
    public sealed class WashCreamPieReaction : ReagentEffect
    {
        public override void Effect(ReagentEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out CreamPiedComponent? creamPied)) return;

            EntitySystem.Get<CreamPieSystem>().SetCreamPied(args.SolutionEntity, creamPied, false);
        }
    }
}
