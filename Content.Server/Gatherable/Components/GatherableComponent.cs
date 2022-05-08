using Content.Shared.Storage;
using Content.Shared.Whitelist;

namespace Content.Server.Anprim14.Gathering.Components;

[RegisterComponent]
[Friend(typeof(GatherableSystem))]
public sealed class GatherableComponent : Component
{
    /// <summary>
    ///     Are we using a mapped loot table?
    /// </summary>
    [DataField("useMappedLoot")] 
    public bool UseMappedLoot;

    /// <summary>
    ///     Whitelist for specifying the kind of tools can be used on a resource
    ///     Supports multiple tags.
    /// </summary>
    [ViewVariables]
    [DataField("whitelist", required: true)] 
    public EntityWhitelist? ToolWhitelist;

    /// <summary>
    ///     Loot table for the resource
    /// </summary>
    [DataField("loot")] 
    public List<EntitySpawnEntry>? Loot = new();

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
    [DataField("mappedLoot")] 
    public Dictionary<string, List<EntitySpawnEntry>>? MappedLoot = new();
}
