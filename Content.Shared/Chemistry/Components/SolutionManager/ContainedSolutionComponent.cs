using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components.SolutionManager;

/// <summary>
/// Component used to relate a solution to its container.
/// </summary>
/// <remarks>
/// When containers are finally ECS'd have this attach to the container entity.
/// The <see cref="Solution.MaxVolume"/> field should then be extracted out into this component.
/// Solution entities would just become an apporpriately composed entity hanging out in the container.
/// Will probably require entities in components being given a relation to associate themselves with their container.
/// TODO: Proper relations system so this can be initialized with a SolutionComponent attached to it :)
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSolutionContainerSystem))]
public sealed partial class ContainedSolutionComponent : Component
{
    /// <summary>
    /// The entity that the solution is contained in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Container;
}
