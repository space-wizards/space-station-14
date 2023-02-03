namespace Content.Client.Singularity.Visualizers;

[RegisterComponent]
[Access(typeof(SingularityVisualizerSystem))]
public sealed class SingularityVisualizerComponent : Component
{
    [DataField("layer")]
    public int Layer { get; } = 0;
}
