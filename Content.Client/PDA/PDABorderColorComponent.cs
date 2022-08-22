namespace Content.Client.PDA;

[RegisterComponent]
public sealed class PDABorderColorComponent : Component
{
    [DataField("borderColor", required: true)]
    public string? BorderColor;


    [DataField("accentHColor")]
    public string? AccentHColor;


    [DataField("accentVColor")]
    public string? AccentVColor;
}
