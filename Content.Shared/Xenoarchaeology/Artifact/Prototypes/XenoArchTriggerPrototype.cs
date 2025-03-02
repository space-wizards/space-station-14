using Content.Shared.Random;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Xenoarchaeology.Artifact.Prototypes;

[Prototype]
public sealed partial class XenoArchTriggerPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [DataField]
    public LocId Tip;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public ComponentRegistry Components = new();
}

[Prototype]
public sealed partial class WeightedRandomXenoArchTriggerPrototype : IWeightedRandomPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(customTypeSerializer: typeof(PrototypeIdDictionarySerializer<float, XenoArchTriggerPrototype>))]
    public Dictionary<string, float> Weights { get; private set; } = new();
}
