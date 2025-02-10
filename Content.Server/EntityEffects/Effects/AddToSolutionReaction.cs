using Content.Shared.Chemistry.EntitySystems;
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

        public override void Effect(EntityEffectBaseArgs args)
        {
            if (args is EntityEffectReagentArgs reagentArgs) {
                if (reagentArgs.Reagent == null)
                    return;

                // TODO see if this is correct
                var solutionContainerSystem = reagentArgs.EntityManager.System<SharedSolutionContainerSystem>();
                if (!solutionContainerSystem.TryGetSolution(reagentArgs.TargetEntity, _solution, out var solutionContainer))
                    return;

                if (solutionContainerSystem.TryAddReagent(solutionContainer.Value, reagentArgs.Reagent.ID, reagentArgs.Quantity, out var accepted))
                    reagentArgs.Source?.RemoveReagent(reagentArgs.Reagent.ID, accepted);

                return;
            }

            // TODO: Someone needs to figure out how to do this for non-reagent effects.
            throw new NotImplementedException();
        }

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
            Loc.GetString("reagent-effect-guidebook-add-to-solution-reaction", ("chance", Probability));
    }
}
