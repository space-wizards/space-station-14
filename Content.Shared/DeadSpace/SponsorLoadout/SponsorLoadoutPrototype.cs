using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.DeadSpace.SponsorLoadout;

[Prototype]
public sealed partial class SponsorLoadoutPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("entity", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string EntityId { get; } = default!;

    [DataField("sponsorOnly")]
    public bool SponsorOnly = false;

    [DataField("whitelistJobs", customTypeSerializer: typeof(PrototypeIdListSerializer<JobPrototype>))]
    public List<string>? WhitelistJobs { get; }

    [DataField("blacklistJobs", customTypeSerializer: typeof(PrototypeIdListSerializer<JobPrototype>))]
    public List<string>? BlacklistJobs { get; }

    [DataField("speciesRestriction")]
    public List<string>? SpeciesRestrictions { get; }
}
