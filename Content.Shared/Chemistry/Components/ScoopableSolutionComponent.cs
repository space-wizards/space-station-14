using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Basically reverse spiking, instead of using the solution-entity on a beaker, you use the beaker on the solution-entity.
/// If there is not enough volume it will stay in the solution-entity rather than spill onto the floor.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ScoopableSolutionSystem))]
public sealed partial class ScoopableSolutionComponent : Component
{
    /// <summary>
    /// Solution name that can be scooped from.
    /// </summary>
    [DataField]
    public string Solution = "default";

    /// <summary>
    /// If true, when the whole solution is scooped up the entity will be deleted.
    /// </summary>
    [DataField]
    public bool Delete = true;

    /// <summary>
    /// Popup to show the user when scooping.
    /// Passed entities "scooped" and "beaker".
    /// </summary>
    [DataField]
    public LocId Popup = "scoopable-component-popup";
}
