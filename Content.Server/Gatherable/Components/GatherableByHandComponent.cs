namespace Content.Server.Gatherable.Components;

[RegisterComponent]
[Access(typeof(GatherableByHandSystem))]
public sealed class GatherableByHandComponent : Component
{
    /// <summary>
    ///     YAML example below
    ///     (Tag1, Tag2, LootTableID1, LootTableID2 are placeholders for example)
    ///     --------------------
    ///     useMappedLoot: true
    ///     whitelist:
    ///       tags:
    ///        - Tag1
    ///        - Tag2
    ///     mappedLoot:
    ///       Tag1: LootTableID1
    ///       Tag2: LootTableID2
    /// </summary>
    [DataField("loot")]
    public Dictionary<string, string>? MappedLoot = new();


    [DataField("mindropcount")]
    public int MinDropCount { get; set; } = 1;

    [DataField("maxdropcount")]
    public int MaxDropCount { get; set; } = 1;

    [DataField("dropradius")]
    public float DropRadius { get; set; } = 1.0f;

    [DataField("harvesttime")]
    public float HarvestTime { get; set; } = 1.0f;
}
