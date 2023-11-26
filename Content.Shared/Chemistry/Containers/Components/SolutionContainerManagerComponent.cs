using Content.Shared.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry.Solutions;

namespace Content.Shared.Chemistry.Containers.Components;

[RegisterComponent]
[Access(typeof(SolutionContainerSystem))]
public sealed partial class SolutionContainerManagerComponent : Component
{
    [DataField("solutions")]
    [Access(typeof(SolutionContainerSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
    public Dictionary<string, Solution> Solutions = new();
}
