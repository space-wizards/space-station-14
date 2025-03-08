using Robust.Shared.Prototypes;

namespace Content.Shared.Random;

/// <summary>
///     Random weighting dataset for solutions, able to specify reagents quantity.
/// </summary>
[Prototype("weightedRandomFillSolution")]
public sealed partial class WeightedRandomFillSolutionPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    ///     List of RandomFills that can be picked from.
    /// </summary>
    [DataField("fills", required: true)]
    public List<RandomFillSolution> Fills = new();
}
