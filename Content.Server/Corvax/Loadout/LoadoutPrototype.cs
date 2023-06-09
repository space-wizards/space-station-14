using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Corvax.Loadout;

[Prototype("loadout")]
public sealed class LoadoutItemPrototype : IPrototype
{
    [IdDataFieldAttribute] public string ID { get; } = default!;

    [DataField("entity", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string EntityId { get; } = default!;
    
    // Corvax-Sponsors-Start
    [DataField("sponsorOnly")]
    public bool SponsorOnly = false;
    // Corvax-Sponsors-End
    
    [DataField("whitelistJobs", customTypeSerializer: typeof(PrototypeIdListSerializer<JobPrototype>))]
    public List<string>? WhitelistJobs { get; }
    
    [DataField("blacklistJobs", customTypeSerializer: typeof(PrototypeIdListSerializer<JobPrototype>))]
    public List<string>? BlacklistJobs { get; }

    [DataField("speciesRestriction")]
    public List<string>? SpeciesRestrictions { get; }
}
