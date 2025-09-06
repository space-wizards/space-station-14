using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.SponsorLoadout;

[Prototype]
public sealed partial class SponsorLoadoutPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("entity", required: true)]
    public EntProtoId EntityId { get; } = default!;

    [DataField]
    public bool SponsorOnly = false;

    [DataField]
    public List<ProtoId<JobPrototype>>? WhitelistJobs { get; }

    [DataField]
    public List<ProtoId<JobPrototype>>? BlacklistJobs { get; }

    [DataField]
    public List<ProtoId<SpeciesPrototype>>? SpeciesRestrictions { get; }
}
