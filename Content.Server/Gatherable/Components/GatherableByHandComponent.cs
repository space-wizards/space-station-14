namespace Content.Server.Gatherable.Components;

[RegisterComponent]
[Access(typeof(GatherableByHandSystem))]
public sealed class GatherableByHandComponent : Component
{
    [DataField("loot")]
    public string? Loot { get; set; }

    [DataField("mindropcount")]
    public int MinDropCount { get; set; } = 1;

    [DataField("maxdropcount")]
    public int MaxDropCount { get; set; } = 1;

    [DataField("dropradius")]
    public float DropRadius { get; set; } = 1.0f;

    [DataField("harvesttime")]
    public float HarvestTime { get; set; } = 1.0f;
}
