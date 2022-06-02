using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;

namespace Content.Server.Chemistry.Components.SolutionManager
{
    [RegisterComponent]
    [Friend(typeof(SolutionContainerSystem))]
    public sealed class SolutionContainerManagerComponent : Component
    {
        [ViewVariables]
        [DataField("solutions")]
        [Friend(typeof(SolutionContainerSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
        public readonly Dictionary<string, Solution> Solutions = new();
    }
}
