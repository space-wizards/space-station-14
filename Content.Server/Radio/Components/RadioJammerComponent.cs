namespace Content.Server.Radio.Components;

[RegisterComponent]
public sealed class RadioJammerComponent : Component
{
    [DataField("enabled"), ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;
    [DataField("distance", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float Distance;
}