using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Components.SolutionManager;

/// <summary>
/// <para>Allows for an entity to have and manage multiple solutions.</para>
/// <para>Spawns additional solutions from their prototypes, and stores them in a container.</para>
/// <para>Also used in the case another component spawns a solution for this entity.</para>
/// <para>Every solution entity this maps should have a <see cref="SolutionComponent"/> to track its state and a <see cref="ContainedSolutionComponent"/> to track its container.</para>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSolutionContainerSystem))]
public sealed partial class SolutionManagerComponent : Component
{
    public static readonly string DefaultContainerId = "solutions";

    /// <summary>
    /// The names of the container for solutions attached to this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Container = DefaultContainerId;

    /// <summary>
    /// A cache of solutions currently attached to this entity.
    /// </summary>
    [ViewVariables]
    [Access(typeof(SharedSolutionContainerSystem), Other = AccessPermissions.None)]
    public Dictionary<string, Entity<SolutionComponent>> Solutions = new ();

    /// <summary>
    /// A list of solution entities to spawn when this component starts up.
    /// </summary>
    [DataField("solutions", readOnly: true)]
    public List<EntProtoId> SolutionEnts = new ();
}
