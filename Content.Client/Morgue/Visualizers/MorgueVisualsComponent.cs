namespace Content.Client.Morgue.Visualizers;

[RegisterComponent]
public sealed class MorgueVisualsComponent : Component
{
    [DataField("lightContents", required: true)]
    public string LightContents = default!;
    [DataField("lightMob", required: true)]
    public string LightMob = default!;
    [DataField("lightSoul", required: true)]
    public string LightSoul = default!;
}

public enum MorgueVisualLayers : byte
{
    Base,
    Light,
}
