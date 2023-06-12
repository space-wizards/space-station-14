namespace Content.Server.Power.Components;

[RegisterComponent]
public sealed class RiggedComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsRigged { get; set; }
}
