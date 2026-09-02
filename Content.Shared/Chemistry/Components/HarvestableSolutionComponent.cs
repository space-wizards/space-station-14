using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Allows a solution on an entity to be transferred into a held refillable container.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(HarvestableSolutionSystem))]
public sealed partial class HarvestableSolutionComponent : Component
{
    /// <summary>
    /// The name of the solution to harvest.
    /// </summary>
    [DataField]
    public string SolutionName = "default";

    /// <summary>
    /// How long harvesting takes.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5);

    [DataField]
    public LocId VerbText = "harvestable-solution-verb";

    [DataField]
    public SpriteSpecifier VerbIcon =
        new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/spill.svg.192dpi.png"));

    /// <summary>
    /// Popup shown when the source has no harvestable solution available.
    /// </summary>
    [DataField]
    public LocId EmptyMessage = "harvestable-solution-empty";

    /// <summary>
    /// Popup shown when the target container cannot accept more solution.
    /// </summary>
    [DataField]
    public LocId TargetFullMessage = "harvestable-solution-target-full";

    /// <summary>
    /// Popup shown after solution is successfully transferred.
    /// </summary>
    [DataField]
    public LocId SuccessMessage = "harvestable-solution-success";
}
