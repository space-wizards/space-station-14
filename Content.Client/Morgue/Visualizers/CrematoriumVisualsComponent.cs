namespace Content.Client.Morgue.Visualizers;

[RegisterComponent]
public sealed class CrematoriumVisualsComponent : Component
{
    [DataField("lightContents", required: true)]
    public string LightContents = default!;
    [DataField("lightBurning", required: true)]
    public string LightBurning = default!;
}

public enum CrematoriumVisualLayers : byte
{
    Base,
    Light,
}
