using Content.Shared.Chemistry.Solution.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSolutionContainerComponent))]
    [ComponentReference(typeof(ISolutionInteractionsComponent))]
    public class SolutionContainerComponent : SharedSolutionContainerComponent
    {
    }
}
