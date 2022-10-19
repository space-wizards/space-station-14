using Robust.Shared.Prototypes;

namespace Content.Shared.Random;

/// <summary>
/// Generic random weighting dataset to use.
/// </summary>
[Prototype("weightedRandom")]
public readonly record struct WeightedRandomPrototype : IPrototype
{
    [IdDataFieldAttribute] public string ID { get; } = default!;

    [DataField("weights")] public readonly Dictionary<string, float> Weights = new();
}
