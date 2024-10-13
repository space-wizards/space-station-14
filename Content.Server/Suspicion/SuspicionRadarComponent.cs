namespace Content.Server.Suspicion;

[RegisterComponent]
public sealed partial class SuspicionRadarComponent : Component
{
    [DataField]
    public bool ShowTraitors = true;
}
