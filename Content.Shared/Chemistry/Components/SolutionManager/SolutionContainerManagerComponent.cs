using Content.Shared.Chemistry.EntitySystems;

namespace Content.Shared.Chemistry.Components.SolutionManager;

[RegisterComponent]
[Access(typeof(SolutionContainerSystem))]
public sealed partial class SolutionContainerManagerComponent : Component
{
    [DataField("solutions")]
    [Access(typeof(SolutionContainerSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
    public Dictionary<string, Solution> Solutions = new();
}
