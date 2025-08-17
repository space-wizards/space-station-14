namespace Content.Client.Power.Visualizers;

[RegisterComponent]
public sealed partial class CableVisualizerComponent : Component
{
    [DataField]
    public string? StatePrefix;

    [DataField]
    public string? ExtraLayerPrefix;
}
