#nullable enable
using Content.Shared.GameObjects.Components.Chemistry;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSolutionContainerComponent))]
    public class SolutionContainerComponent : SharedSolutionContainerComponent
    {

    }
}
