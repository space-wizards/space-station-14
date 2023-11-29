using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;

namespace Content.Server.Chemistry.Containers.Components;

/// <summary>
/// Component used to spawn solution entities on <see cref="MapInitEvent"/>.
/// Replaces itself with a <see cref="SolutionContainerComponent"/> during map initialization.
/// </summary>
[RegisterComponent]
[Access(typeof(SolutionContainerSystem))]
public sealed partial class SolutionContainerManagerComponent : Component
{
    /// <summary>
    /// The set of solution prototypes that are going to be used to spawn their respective solution entities during <see cref="MapInitEvent"/>.
    /// </summary>
    [DataField]
    public Dictionary<string, Solution> Solutions = new(SolutionContainerComponent.DefaultCapacity);
}
