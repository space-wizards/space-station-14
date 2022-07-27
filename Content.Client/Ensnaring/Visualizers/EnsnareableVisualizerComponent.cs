using Robust.Shared.Utility;

namespace Content.Client.Ensnaring.Visualizers;
[RegisterComponent]
[Access(typeof(EnsnareableVisualizerSystem))]
public sealed class EnsnareableVisualizerComponent : Component
{
    [DataField("sprite")]
    public string? Sprite;

    [DataField("state")]
    public string? State;
}
