using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

[Prototype]
public sealed partial class XenoArchTriggerPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [DataField]
    public LocId Hint;

    [DataField]
    public ComponentRegistry Components = new();
}
