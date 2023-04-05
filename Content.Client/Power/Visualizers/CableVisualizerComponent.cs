namespace Content.Client.Power.Visualizers;

[RegisterComponent]
public sealed class CableVisualizerComponent : Component
{
    [DataField("statePrefix")]
    public string? StatePrefix;
}
