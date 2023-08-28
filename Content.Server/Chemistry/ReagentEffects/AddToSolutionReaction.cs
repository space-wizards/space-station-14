using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects
{
    [UsedImplicitly]
    public sealed partial class AddToSolutionReaction : ReagentEffect
    {
        [DataField("solution")]
        private string _solution = "reagents";

        public override void Effect(ReagentEffectArgs args)
        {
            if (args.Reagent == null)
                return;

            // TODO see if this is correct
            if (!EntitySystem.Get<SolutionContainerSystem>()
                    .TryGetSolution(args.SolutionEntity, _solution, out var solutionContainer))
                return;

            if (EntitySystem.Get<SolutionContainerSystem>()
                .TryAddReagent(args.SolutionEntity, solutionContainer, args.Reagent.ID, args.Quantity, out var accepted))
                args.Source?.RemoveReagent(args.Reagent.ID, accepted);
        }

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
            Loc.GetString("reagent-effect-guidebook-missing", ("chance", Probability));
    }
}
