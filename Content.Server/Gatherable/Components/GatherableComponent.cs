using Content.Shared.Storage;
using Content.Shared.Whitelist;

namespace Content.Server.Gatherable.Components;

[RegisterComponent]
[Friend(typeof(GatherableSystem))]
public sealed class GatherableComponent : Component
{
    /// <summary>
    ///     Whitelist for specifying the kind of tools can be used on a resource
    ///     Supports multiple tags.
    /// </summary>
    [ViewVariables]
    [DataField("whitelist", required: true)] 
    public EntityWhitelist? ToolWhitelist;

    /// <summary>
    ///     If this is defined, loot table will must be mapped
    ///     YAML example below
    ///     (Tag1, Tag2, Entity1, Entity2 are placeholders for example)
    ///     --------------------
    ///     useMappedLoot: true
    ///     whitelist:
    ///       tags:
    ///        - Tag1
    ///        - Tag2
    ///     mappedLoot:
    ///       Tag1:
    ///         - id: EntityID1
    ///       FishingPole:
    ///         - id: EntityID2
    /// </summary>
    [DataField("loot")] 
    public Dictionary<string, List<EntitySpawnEntry>>? MappedLoot = new();
}
