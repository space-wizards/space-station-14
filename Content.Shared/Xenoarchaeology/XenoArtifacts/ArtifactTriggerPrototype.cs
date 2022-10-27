using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.XenoArtifacts;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype("artifactTrigger")]
[DataDefinition]
public sealed class ArtifactTriggerPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("components")]
    public EntityPrototype.ComponentRegistry Components = new();

    [DataField("targetDepth")]
    public int TargetDepth = 0;
}
