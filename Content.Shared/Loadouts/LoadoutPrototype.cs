using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Loadouts;

[Prototype("loadout")]
public sealed class LoadoutPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    ///     Which tab category to put this under.
    /// </summary>
    [DataField("category", customTypeSerializer:typeof(PrototypeIdSerializer<LoadoutCategoryPrototype>))]
    public string Category = "Uncategorized";

    /// <summary>
    ///     The item to give.
    /// </summary>
    [DataField("items", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>), required: true)]
    public List<string> Items = new();


    /// <summary>
    ///     The point cost of this loadout.
    /// </summary>
    [DataField("cost")]
    public int Cost = 1;

    /// <summary>
    ///     Should this item override other items in the same slot?
    /// </summary>
    [DataField("exclusive")]
    public bool Exclusive;


    /// <summary>
    ///     Don't apply this loadout to entities this whitelist IS NOT valid for.
    /// </summary>
    [DataField("entityWhitelist")]
    public EntityWhitelist? EntityWhitelist;

    /// <summary>
    ///     Don't apply this loadout to entities this whitelist IS valid for. (hence, a blacklist)
    /// </summary>
    [DataField("entityBlacklist")]
    public EntityWhitelist? EntityBlacklist;

    /// <summary>
    ///     Need one of these jobs to give loadout.
    /// </summary>
    [DataField("jobWhitelist")]
    public List<string>? JobWhitelist;

    /// <summary>
    ///     Need none of these jobs to give loadout.
    /// </summary>
    [DataField("jobBlacklist")]
    public List<string>? JobBlacklist;

    /// <summary>
    ///     Don't apply this loadout to entities this whitelist IS NOT valid for.
    /// </summary>
    [DataField("speciesWhitelist")]
    public List<string>? SpeciesWhitelist;

    /// <summary>
    ///     Don't apply this loadout to entities this whitelist IS valid for. (hence, a blacklist)
    /// </summary>
    [DataField("speciesBlacklist")]
    public List<string>? SpeciesBlacklist;
}
