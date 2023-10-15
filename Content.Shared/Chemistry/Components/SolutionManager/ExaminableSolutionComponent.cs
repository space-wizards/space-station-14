namespace Content.Shared.Chemistry.Components.SolutionManager;

[RegisterComponent]
public sealed partial class ExaminableSolutionComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "default";

    /// <summary>
    /// Replace "covered" with "contains" in examine text by switching Loc
    /// Used in entities with <see cref="Stains.StainableComponent"/>
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool UseAltExamineText = false;
}
