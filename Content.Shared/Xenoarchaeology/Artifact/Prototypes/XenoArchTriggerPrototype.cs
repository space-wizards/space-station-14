using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.Artifact.Prototypes;

/// <summary> Proto for xeno artifact triggers - markers, which event could trigger node to unlock it. </summary>
[Prototype]
public sealed partial class XenoArchTriggerPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Tip for user on how to activate this trigger.
    /// </summary>
    [DataField]
    public LocId Tip;

    /// <summary>
    /// Whitelist, describing for which subtype of artifacts this trigger could be used.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// List of components that represent ways to trigger node.
    /// </summary>
    [DataField]
    public ComponentRegistry Components = new();
}
