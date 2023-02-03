namespace Content.Client.Visualizer;

[RegisterComponent]
[Access(typeof(FoldableVisualizerSystem))]
public sealed class FoldableVisualizerComponent : Component
{
    [DataField("key")]
    public string Key = default!;
}
