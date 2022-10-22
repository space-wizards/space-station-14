using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects
{
    [UsedImplicitly]
    public sealed class AddToSolutionReaction : ReagentEffect
    {
        [DataField("solution")]
        private string _solution = "reagents";

        public override void Effect(ReagentEffectArgs args)
        {
            if (args.Reagent == null)
                return;

            // TODO see if this is correct
            var solSys = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SolutionContainerSystem>();
            if (solSys.TryGetSolution(args.SolutionEntity, _solution, out var solutionContainer)
            &&  solSys.TryAddReagent(args.SolutionEntity, solutionContainer, args.Reagent.ID, args.Quantity, out var accepted))
                args.Source?.RemoveReagent(args.Reagent.ID, accepted);
        }
    }
}
