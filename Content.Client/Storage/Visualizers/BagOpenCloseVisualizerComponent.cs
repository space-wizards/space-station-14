namespace Content.Client.Storage.Visualizers;

[RegisterComponent]
[Access(typeof(BagOpenCloseVisualizerSystem))]
public sealed class BagOpenCloseVisualizerComponent : Component
{
    public const string OpenIconLayer = "openIcon";
    [DataField("openIcon")]
    public string? OpenIcon;
}
