namespace Content.Shared.Lube;

[RegisterComponent]
public sealed partial class LubedComponent : Component
{
    [DataField, ViewVariables]
    public int SlipsLeft;

    [DataField, ViewVariables]
    public int SlipStrength;
}
