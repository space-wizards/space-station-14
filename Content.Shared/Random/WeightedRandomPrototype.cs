using Robust.Shared.Prototypes;

namespace Content.Shared.Random;

/// <summary>
/// Generic random weighting dataset to use.
/// </summary>
[Prototype("weightedRandom")]
public sealed class WeightedRandomPrototype : IWeightedRandomPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("weights")]
    public Dictionary<string, float> Weights { get; } = new();
}
