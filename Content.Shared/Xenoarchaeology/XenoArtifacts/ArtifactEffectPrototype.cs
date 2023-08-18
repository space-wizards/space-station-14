using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.XenoArtifacts;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype("artifactEffect")]
[DataDefinition]
public sealed partial class ArtifactEffectPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Components that are added to the artifact when the specfic effect is active.
    /// These are removed after the node is exited and the effect is changed.
    /// </summary>
    [DataField("components", serverOnly: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Components that are permanently added to an entity when the effect's node is entered.
    /// </summary>
    [DataField("permanentComponents")]
    public ComponentRegistry PermanentComponents = new();

    //TODO: make this a list so we can have multiple target depths
    [DataField("targetDepth")]
    public int TargetDepth = 0;

    [DataField("effectHint")]
    public string? EffectHint;

    [DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    [DataField("blacklist")]
    public EntityWhitelist? Blacklist;
}
