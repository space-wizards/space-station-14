using Content.Shared.Mining;
using Robust.Shared.Prototypes;

namespace Content.Shared.Random;

/// <summary>
/// Linter-friendly version of weightedRandom for Ore prototypes.
/// </summary>
[Prototype]
public sealed partial class WeightedRandomOrePrototype : IWeightedRandomPrototype<OrePrototype>
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public Dictionary<ProtoId<OrePrototype>, float> Weights { get; private set; } = new();
}
