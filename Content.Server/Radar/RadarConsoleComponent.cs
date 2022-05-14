namespace Content.Server.Radar;

[RegisterComponent]
public sealed class RadarConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("range")]
    public float Range = 256f;
}
