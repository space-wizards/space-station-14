using Robust.Shared.Prototypes;

namespace Content.Shared.Random;

/// <summary>
/// Generic random weighting dataset to use.
/// </summary>
[Prototype("weightedRandom")]
public sealed class WeightedRandomPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("weights")]
    public Dictionary<string, float> Weights = new();
}
