using Content.Shared.Whitelist;

namespace Content.Server.Gatherable.Components;

[RegisterComponent]
[Access(typeof(GatherableSystem))]
public sealed partial class GatherableComponent : Component
{
    /// <summary>
    ///     Whitelist for specifying the kind of tools can be used on a resource
    ///     Supports multiple tags.
    /// </summary>
    [DataField("whitelist", required: true)]
    public EntityWhitelist? ToolWhitelist;

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
}
