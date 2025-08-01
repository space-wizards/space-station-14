namespace Content.Shared.Chemistry.Components.SolutionManager;

/// <summary>
///     Component for examining a solution with shift click.
/// </summary>
[RegisterComponent]
public sealed partial class ExaminableSolutionComponent : Component
{
    /// <summary>
    ///     The solution being examined.
    /// </summary>
    [DataField]
    public string Solution = "default";

    /// <summary>
    ///     If true, the solution must be held to be examined.
    /// </summary>
    [DataField]
    public bool HeldOnly;

    /// <summary>
    ///     If true, the examine text will give an approximation of the remaining solution.
    ///     If false, the exact unit count will be shown.
    /// </summary>
    [DataField]
    public bool VagueExamine;
}
