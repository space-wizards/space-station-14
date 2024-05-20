using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects
{
    [UsedImplicitly]
    public sealed partial class AddToSolutionReaction : EntityEffect
    {
        [DataField("solution")]
        private string _solution = "reagents";

        public override void Effect(EntityEffectArgs args)
        {
            if (args.Reagent == null)
                return;

            // TODO see if this is correct
            var solutionContainerSystem = args.EntityManager.System<SolutionContainerSystem>();
            if (!solutionContainerSystem.TryGetSolution(args.TargetEntity, _solution, out var solutionContainer))
                return;

            if (solutionContainerSystem.TryAddReagent(solutionContainer.Value, args.Reagent.ID, args.Quantity, out var accepted))
                args.Source?.RemoveReagent(args.Reagent.ID, accepted);
        }

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
            Loc.GetString("reagent-effect-guidebook-missing", ("chance", Probability));
    }
}
