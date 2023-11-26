namespace Content.Shared.Chemistry.Containers.Components;

[RegisterComponent]
public sealed partial class ExaminableSolutionComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("solution")]
    public string Solution { get; set; } = "default";
}
