using Robust.Shared.Prototypes;

namespace Content.Shared.Random;

/// <summary>
/// Generic random weighting dataset to use.
/// </summary>
[Prototype("weightedRandomQuantity")]
public sealed class WeightedRandomQuantityPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("weights")]
    public Dictionary<string, float> Weights = new();

    [DataField("quantities")]
    public Dictionary<string, float> Quantities = new();
}
