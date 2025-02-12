namespace Content.Client.Smoking;

[RegisterComponent]
public sealed partial class BurnStateVisualsComponent : Component
{
    [DataField]
    public string BurntIcon = "burnt-icon";
    [DataField]
    public string LitIcon = "lit-icon";
    [DataField]
    public string UnlitIcon = "icon";
}

