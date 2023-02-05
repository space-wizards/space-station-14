namespace Content.Client.Explosion;

[RegisterComponent]
[Access(typeof(ClusterGrenadeVisualizerSystem))]
public sealed class ClusterGrenadeVisualsComponent : Component
{
    [DataField("state")]
    public string? State;
}
