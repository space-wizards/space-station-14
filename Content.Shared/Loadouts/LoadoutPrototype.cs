using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Loadouts;

[Prototype("loadout")]
public sealed class LoadoutPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    ///     Which tab category to put this under
    /// </summary>
    [DataField(customTypeSerializer:typeof(PrototypeIdSerializer<LoadoutCategoryPrototype>))]
    public string Category = "Uncategorized";

    /// <summary>
    ///     The item to give
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>), required: true)]
    public List<string> Items = new();


    /// <summary>
    ///     The point cost of this loadout
    /// </summary>
    [DataField]
    public int Cost = 1;

    /// <summary>
    ///     Should this item override other items in the same slot?
    /// </summary>
    [DataField]
    public bool Exclusive;


    /// <summary>
    ///     Don't apply this loadout to entities this whitelist IS NOT valid for
    /// </summary>
    [DataField]
    public EntityWhitelist? EntityWhitelist;

    /// <summary>
    ///     Don't apply this loadout to entities this whitelist IS valid for (hence, a blacklist)
    /// </summary>
    [DataField]
    public EntityWhitelist? EntityBlacklist;

    /// <summary>
    ///     Need one of these jobs to give loadout
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<JobPrototype>))]
    public List<string>? JobWhitelist;

    /// <summary>
    ///     Need none of these jobs to give loadout
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<JobPrototype>))]
    public List<string>? JobBlacklist;

    /// <summary>
    ///     Don't apply this loadout to entities this whitelist IS NOT valid for
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<SpeciesPrototype>))]
    public List<string>? SpeciesWhitelist;

    /// <summary>
    ///     Don't apply this loadout to entities this whitelist IS valid for
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<SpeciesPrototype>))]
    public List<string>? SpeciesBlacklist;
}
