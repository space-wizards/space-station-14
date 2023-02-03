namespace Content.Client.Light.Visualizers;

[RegisterComponent]
[Access(typeof(ExpendableLightVisualizerSystem))]
public sealed class ExpendableLightVisualizerComponent : Component
{
    [DataField("iconStateSpent")]
    public string? IconStateSpent { get; set; }

    [DataField("iconStateOn")]
    public string? IconStateLit { get; set; }
}
