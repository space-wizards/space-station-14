using Content.Shared.Item;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.XenoArtifacts;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype("artifactTrigger")]
[DataDefinition]
public sealed partial class ArtifactTriggerPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("components", serverOnly: true)]
    public ComponentRegistry Components = new();

    [DataField("targetDepth")]
    public int TargetDepth = 0;

    [DataField("triggerHint")]
    public string? TriggerHint;

    /// <summary>
    /// Artifact types that can have this trigger, leave blank for all
    /// </summary>
    [DataField("originWhitelist")]
    public List<String>? OriginWhitelist;

    [DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    [DataField("blacklist")]
    public EntityWhitelist? Blacklist;
}
