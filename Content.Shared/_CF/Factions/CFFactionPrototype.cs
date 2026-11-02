using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.CF.Factions;

/// <summary>
/// A CarpFinder faction. Relations not listed here are <see cref="CFAlliance.Neutral"/>.
/// </summary>
[Prototype("cfFaction")]
public sealed partial class CFFactionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    /// <summary>How two members of this same faction treat each other. Adventure teams are a party.</summary>
    [DataField]
    public CFAlliance Same = CFAlliance.Party;

    /// <summary>Jobs that join this faction when a player spawns.</summary>
    [DataField]
    public List<ProtoId<JobPrototype>> Jobs = new();

    [DataField]
    public HashSet<ProtoId<CFFactionPrototype>> Party = new();

    [DataField]
    public HashSet<ProtoId<CFFactionPrototype>> Opposition = new();
}
