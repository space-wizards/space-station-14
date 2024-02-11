namespace Content.Server.Research.Components;

[RegisterComponent]
public sealed partial class ResearchPointSourceComponent : Component
{
    [DataField("pointspersecond"), ViewVariables(VVAccess.ReadWrite)]
    public int PointsPerSecond;

    [DataField("active"), ViewVariables(VVAccess.ReadWrite)]
    public bool Active;
}
