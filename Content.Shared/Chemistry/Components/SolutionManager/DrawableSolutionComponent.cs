namespace Content.Shared.Chemistry.Components.SolutionManager;

/// <summary>
/// Denotes the solution that can be removed with syringes.
/// </summary>
[RegisterComponent]
public sealed partial class DrawableSolutionComponent : Component
{
    /// <summary>
    /// Solution name that can be removed with syringes.
    /// </summary>
    [DataField]
    public string Solution = "default";
}
