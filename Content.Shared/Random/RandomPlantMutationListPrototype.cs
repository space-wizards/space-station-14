using Robust.Shared.Prototypes;

namespace Content.Shared.Random;

/// <summary>
///     Random weighting dataset for solutions, able to specify reagents quantity.
/// </summary>
[Prototype("RandomPlantMutationList")]
public sealed partial class RandomPlantMutationListPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    ///     List of RandomFills that can be picked from.
    /// </summary>
    [DataField("mutations", required: true, serverOnly: true)]
    public List<RandomPlantMutation> mutations = new();
}
