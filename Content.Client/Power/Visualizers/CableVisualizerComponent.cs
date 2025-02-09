namespace Content.Client.Power.Visualizers;

[RegisterComponent]
public sealed partial class CableVisualizerComponent : Component
{
    [DataField("statePrefix")]
    public string? StatePrefix;

    [DataField("extraLayerPrefix")]
    public string? ExtraLayerPrefix;
}
