using Content.Shared.EntityList;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.Gatherable.Components;

[RegisterComponent]
[Access(typeof(GatherableSystem))]
public sealed partial class GatherableComponent : Component
{
    /// <summary>
    ///     Whitelist for specifying the kind of tools can be used on a resource
    ///     Supports multiple tags.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist? ToolWhitelist;

    /// <summary>
    ///     YAML example below
    ///     (Tag1, Tag2, LootTableID1, LootTableID2 are placeholders for example)
    ///     --------------------
    ///     useMappedLoot: true
    ///     toolWhitelist:
    ///       tags:
    ///        - Tag1
    ///        - Tag2
    ///     loot:
    ///       Tag1: LootTableID1
    ///       Tag2: LootTableID2
    /// </summary>
    [DataField]
    public Dictionary<string, ProtoId<EntityLootTablePrototype>>? Loot = new();

    /// <summary>
    /// Random shift of the appearing entity during gathering
    /// </summary>
    [DataField]
    public float GatherOffset = 0.3f;
}
