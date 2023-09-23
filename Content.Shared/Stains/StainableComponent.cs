namespace Content.Shared.Stains;

[RegisterComponent]
public partial class StainableComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "stains";
}
