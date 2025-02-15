using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

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

    /// <summary>
    /// Icon to display with the harvesting verb.
    /// </summary>
    [DataField]
    public SpriteSpecifier VerbIcon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/spill.svg.192dpi.png"));

    /// <summary>
    /// LocId of popup message displayed when there is nothing to harvest.
    /// </summary>
    [DataField]
    public LocId EmptyMessage = "harvestable-solution-component-harvest-empty";

    /// <summary>
    /// LocId of popup message displayed when the harvest fails because the target container is full.
    /// </summary>
    [DataField]
    public LocId TargetFullMessage = "harvestable-solution-component-harvest-target-full";

    /// <summary>
    /// LocId of popup message displayed when successfully harvesting from the solution.
    /// </summary>
    [DataField]
    public LocId SuccessMessage = "harvestable-solution-component-harvest-success";
}
