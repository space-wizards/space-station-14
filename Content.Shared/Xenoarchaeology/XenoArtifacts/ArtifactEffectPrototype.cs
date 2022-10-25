using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.XenoArtifacts;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype("artifactEffect")]
[DataDefinition]
public sealed class ArtifactEffectPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("components")]
    public EntityPrototype.ComponentRegistry Components = new();
}
