namespace Content.Server.Research.Components;

[RegisterComponent]
public sealed partial class ResearchPointSourceComponent : Component
{
    [DataField, ViewVariables]
    public int PointsPerSecond;

    [DataField, ViewVariables]
    public bool Active;
}
