namespace Content.Server.Power.Components;

[RegisterComponent]
public sealed class RiggableComponent : Component
{
    public const string SolutionName = "battery";

    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsRigged { get; set; }
}
