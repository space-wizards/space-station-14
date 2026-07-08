using Robust.Shared.Prototypes;

namespace Content.Shared.Random;

/// <summary>
/// Random weighting dataset for solutions, able to specify reagents quantity.
/// </summary>
[Prototype]
public sealed partial class RandomPlantMutationListPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// List of RandomFills that can be picked from.
    /// </summary>
    [DataField(required: true)]
    public List<RandomPlantMutation> Mutations = new();
}
