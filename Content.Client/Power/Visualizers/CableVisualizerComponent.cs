namespace Content.Client.Power.Visualizers;

[RegisterComponent]
public sealed class CableVisualizerComponent : Component
{
    [DataField("baseState")]
    public string? StateBase;
}
