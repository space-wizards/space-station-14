namespace Content.Shared.Chemistry.Components.SolutionManager;

[RegisterComponent]
public sealed partial class ExaminableSolutionComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "default";

    /// <summary>
    /// If false then the hidden solution is always visible.
    /// </summary>
    [DataField]
    public bool HeldOnly;

    /// <summary>
    /// If true then the solution needs to be open to show how full it is.
    /// </summary>
    [DataField]
    public bool Opaque;

    /// <summary>
    /// Should we only give an estimate of fullness instead of the exact value?
    /// </summary>
    [DataField]
    public bool Estimate;
}
