using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Xenoarchaeology.Artifact.Prototypes;

/// <summary>
/// Container for list of xeno artifact effects and their respective weights to be used in case randomly rolling effect is required.
/// </summary>
[Prototype]
public sealed partial class WeightedRandomXenoArchEffectPrototype : IWeightedRandomPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(customTypeSerializer: typeof(PrototypeIdDictionarySerializer<float, EntityPrototype>))]
    public Dictionary<string, float> Weights { get; private set; } = new();
}
