using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Components.SolutionManager;

/// <summary>
/// <para>A map of the solution entities contained within this entity.</para>
/// <para>Every solution entity this maps should have a <see cref="SolutionComponent"/> to track its state and a <see cref="ContainedSolutionComponent"/> to track its container.</para>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSolutionContainerSystem))]
public sealed partial class SolutionManagerComponent : Component
{
    public static readonly string DefaultContainerId = "solutions";

    /// <summary>
    /// The names of each solution container attached to this entity.
    /// Actually accessing them must be done via <see cref="ContainerManagerComponent"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Container = DefaultContainerId;

    /// <summary>
    /// A cache of solutions
    /// </summary>
    [ViewVariables]
    public Dictionary<string, Entity<SolutionComponent>> Solutions = new ();

    /// <summary>
    /// A list of solution entities to spawn when this component starts up.
    /// </summary>
    [DataField("solutions")]
    public List<EntProtoId> SolutionEnts = new ();
}
