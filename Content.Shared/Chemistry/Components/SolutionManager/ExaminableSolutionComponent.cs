namespace Content.Shared.Chemistry.Components.SolutionManager;

[RegisterComponent]
public sealed partial class ExaminableSolutionComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "default";

    [DataField]
    public bool HeldOnly = false;
}
