using Content.Shared.Item;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

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

    [DataField("components", serverOnly: true)]
    public EntityPrototype.ComponentRegistry Components = new();

    [DataField("targetDepth")]
    public int TargetDepth = 0;

    [DataField("triggerHint")]
    public string? TriggerHint;

    /// <summary>
    ///     Should this trigger be restricted from artifacts with <see cref="ItemComponent"/>?
    /// </summary>
    [DataField("restrictItems")]
    public bool RestrictItems = false;

    /// <summary>
    ///     Should this trigger be restricted from artifacts that do not have <see cref="ItemComponent"/>?
    /// </summary>
    [DataField("restrictStructures")]
    public bool RestrictStructures = false;
}
