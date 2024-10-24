namespace Content.Server.Light.Components;

[RegisterComponent]
public sealed partial class SetRoofComponent : Component
{
    [DataField(required: true)]
    public bool Value;
}
