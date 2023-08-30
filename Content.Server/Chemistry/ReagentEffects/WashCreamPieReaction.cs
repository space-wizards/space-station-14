using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Nutrition.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects
{
    [UsedImplicitly]
    public sealed partial class WashCreamPieReaction : ReagentEffect
    {
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-wash-cream-pie-reaction", ("chance", Probability));

        public override void Effect(ReagentEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out CreamPiedComponent? creamPied)) return;

            EntitySystem.Get<CreamPieSystem>().SetCreamPied(args.SolutionEntity, creamPied, false);
        }
    }
}
