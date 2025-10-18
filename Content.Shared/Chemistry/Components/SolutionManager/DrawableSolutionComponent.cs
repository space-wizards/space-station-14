namespace Content.Shared.Chemistry.Components.SolutionManager;

/// <summary>
///     Denotes the solution that can removed  be with syringes.
/// </summary>
[RegisterComponent]
public sealed partial class DrawableSolutionComponent : Component
{
    /// <summary>
    /// Solution name that can be removed with syringes.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "default";
}
