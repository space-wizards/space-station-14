using Content.Shared.Tools.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Tools.Components;

/// <summary>
/// Used for something that can be refined by some tool with splitting of solution in process.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ToolRefinableSystem))]
public sealed partial class ToolRefinableSolutionComponent : Component
{
    /// <summary>
    /// Name of the solution that stores reagents to be split.
    /// </summary>
    [DataField]
    public string SolutionToSplit = "food";

    /// <summary>
    /// Name of the solution to which reagents from <see cref="SolutionToSplit"/> should be placed.
    /// </summary>
    [DataField]
    public string SolutionToSet = "food";
}
