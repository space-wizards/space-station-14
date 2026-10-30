using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Random;

/// <summary>
/// Linter-friendly version of weightedRandom for Species prototypes.
/// </summary>
[Prototype]
public sealed partial class WeightedRandomSpeciesPrototype : IWeightedRandomPrototype<SpeciesPrototype>
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public Dictionary<ProtoId<SpeciesPrototype>, float> Weights { get; private set; } = new();
}
