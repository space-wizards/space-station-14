using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Enables a solution to be extracted from the entity into
/// a held container by using a verb on the entity.
/// The target container must have a <see cref="RefillableSolutionComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(HarvestableSolutionSystem))]
public sealed partial class HarvestableSolutionComponent : Component
{
    /// <summary>
    /// Name of the target solution from which to harvest.
    /// </summary>
    [DataField]
    public string Solution = "default";

    /// <summary>
    /// Length of the DoAfter to harvest from this entity.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5);

    /// <summary>
    /// LocId of label to display on the harvesting verb.
    /// </summary>
    [DataField]
    public LocId VerbText = "harvestable-solution-component-harvest-verb";

    // TODO: Verb icon

    /// <summary>
    /// LocId of popup message displayed when there is nothing to harvest.
    /// </summary>
    [DataField]
    public LocId EmptyMessage = "harvestable-solution-component-harvest-empty";

    /// <summary>
    /// LocId of popup message displayed when successfully harvesting from the solution.
    /// </summary>
    [DataField]
    public LocId SuccessMessage = "harvestable-solution-component-harvest-success";
}
