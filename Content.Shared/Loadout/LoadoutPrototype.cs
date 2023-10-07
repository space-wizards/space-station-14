using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Loadout;

[Prototype("loadout")]
public sealed class LoadoutPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    ///     Entity that will receive player, also provide name and description for menu
    /// </summary>
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype { get; } = default!;

    /// <summary>
    ///     Which tab category to put this under.
    /// </summary>
    [DataField("category")]
    public string Category { get; private set; } = "loadout-category-uncategorized";

    /// <summary>
    ///     The point cost of this loadout
    /// </summary>
    [DataField("cost")]
    public int Cost = 1;

    /// <summary>
    ///     Jobs that allowed to have this loadout
    /// </summary>
    [DataField("whitelistJobs", customTypeSerializer: typeof(PrototypeIdListSerializer<JobPrototype>))]
    public List<string>? WhitelistJobs { get; }

    /// <summary>
    ///     Jobs that forbidden to have this loadout
    /// </summary>
    [DataField("blacklistJobs", customTypeSerializer: typeof(PrototypeIdListSerializer<JobPrototype>))]
    public List<string>? BlacklistJobs { get; }

    /// <summary>
    ///     Species that forbidden to have this loadout
    /// </summary>
    [DataField("speciesRestriction")]
    public List<string>? SpeciesRestrictions { get; }

    /// <summary>
    ///     Should this item override other items in the same slot?
    /// </summary>
    [DataField("exclusive")]
    public bool Exclusive = false;
	
	/// <summary>
    ///     Should this item be only available for sponsors?
    /// </summary>
    [DataField("sponsorOnly")]
    public bool SponsorOnly = false;
}
