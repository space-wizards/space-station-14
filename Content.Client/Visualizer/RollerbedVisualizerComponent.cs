namespace Content.Client.Visualizer;

[RegisterComponent]
[Access(typeof(RollerbedVisualizerSystem))]
public sealed class RollerbedVisualizerComponent : Component
{
    [DataField("key")]
    public string Key = default!;
}
