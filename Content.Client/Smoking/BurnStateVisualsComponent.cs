namespace Content.Client.Smoking;

[RegisterComponent]
public sealed class BurnStateVisualsComponent : Component
{
    [DataField("burntIcon")]
    public string BurntIcon = "burnt-icon";
    [DataField("litIcon")]
    public string LitIcon = "lit-icon";
    [DataField("unlitIcon")]
    public string UnlitIcon = "icon";
}

