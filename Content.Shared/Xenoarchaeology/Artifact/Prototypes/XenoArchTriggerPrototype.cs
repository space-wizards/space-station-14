using Content.Shared.Random;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Xenoarchaeology.Artifact.Prototypes;

/// <summary> Proto for xeno artifact triggers - markers, which event could trigger node to unlock it. </summary>
[Prototype]
public sealed partial class XenoArchTriggerPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

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

/// <summary>
/// Container for list of xeno artifact triggers and their respective weights to be used in case randomly rolling trigger is required.
/// </summary>
[Prototype]
public sealed partial class WeightedRandomXenoArchTriggerPrototype : IWeightedRandomPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(customTypeSerializer: typeof(PrototypeIdDictionarySerializer<float, XenoArchTriggerPrototype>))]
    public Dictionary<string, float> Weights { get; private set; } = new();
}
