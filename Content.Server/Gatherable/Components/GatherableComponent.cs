using Content.Shared.Whitelist;

namespace Content.Server.Gatherable.Components;

[RegisterComponent]
[Access(typeof(GatherableSystem))]
public sealed class GatherableComponent : Component
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
    ///     whitelist:
    ///       tags:
    ///        - Tag1
    ///        - Tag2
    ///     mappedLoot:
    ///       Tag1: LootTableID1
    ///       Tag2: LootTableID2
    /// </summary>
    [DataField("mappedLoot")]
    public Dictionary<string, string>? MappedLoot = new();

    /// <summary>
    ///     The amount of time in seconds it takes to complete the gathering action by hand.
    /// </summary>
    [DataField("harvestTimeByHand")]
    public float harvestTimeByHand = 1.0f;

    /// <summary>
    ///     The radius of the circle that loot entities can be randomly spawned in when gathered.
    ///     Centered on the entity.
    /// </summary>
    [DataField("dropRadius")]
    public float DropRadius = 0.3f;

}
