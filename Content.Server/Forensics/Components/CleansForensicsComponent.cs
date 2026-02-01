using Content.Shared.Forensics;
using Robust.Shared.Prototypes;

namespace Content.Server.Forensics;

/// <summary>
/// This component is for items that can clean up forensic evidence
/// </summary>
[RegisterComponent]
public sealed partial class CleansForensicsComponent : Component
{
    /// <summary>
    /// How long it takes to wipe prints/blood/etc. off of things using this entity
    /// </summary>
    [DataField]
    public float CleanDelay = 12.0f;

    /// <summary>
    /// The adjective for the cleaning agent, e.g slippery
    /// </summary>
    [DataField]
    public LocId AgentAdjective = "cleaning-agent-unknown";

    /// <summary>
    /// An optional color for this cleaning agent
    /// </summary>
    [DataField]
    public string? AgentColor;

    /// <summary>
    /// A blacklist of evidence that this CANNOT clean
    /// </summary>
    [DataField]
    public List<ProtoId<ForensicEvidencePrototype>> Blacklist = [];
}
