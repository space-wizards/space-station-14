#nullable enable
using Content.Shared.GameObjects.Components.Chemistry;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSolutionContainerComponent))]
    [ComponentReference(typeof(ISolutionInteractionsComponent))]
    public class SolutionContainerComponent : SharedSolutionContainerComponent
    {
    }
}
