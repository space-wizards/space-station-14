using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;

namespace Content.Server.Chemistry.Components.SolutionManager
{
    [RegisterComponent]

    public sealed class SolutionContainerManagerComponent : Component
    {
        [ViewVariables]
        [DataField("solutions")]
         // FIXME Friends
        public readonly Dictionary<string, Solution> Solutions = new();
    }
}
