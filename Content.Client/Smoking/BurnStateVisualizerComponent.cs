namespace Content.Client.Smoking;

[RegisterComponent]
[Access(typeof(BurnStateVisualizerSystem))]
public sealed class BurnStateVisualizerComponent : Component
{
    [DataField("burntIcon")]
    public string BurntIcon = "burnt-icon";
    [DataField("litIcon")]
    public string LitIcon = "lit-icon";
    [DataField("unlitIcon")]
    public string UnlitIcon = "icon";
}
