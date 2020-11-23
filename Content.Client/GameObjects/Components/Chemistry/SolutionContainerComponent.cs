using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSolutionContainerComponent))]
    public class SolutionContainerComponent : SharedSolutionContainerComponent
    {
        public override bool CanAddSolution(Solution solution)
        {
            // TODO CLIENT
            return false;
        }

        public override bool TryAddSolution(Solution solution, bool skipReactionCheck = false, bool skipColor = false)
        {
            // TODO CLIENT
            return false;
        }

        public override bool TryRemoveReagent(string reagentId, ReagentUnit quantity)
        {
            // TODO CLIENT
            return false;
        }
    }
}
