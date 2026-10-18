using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Random weighting dataset for solutions, able to specify reagents quantity.
/// </summary>
[Prototype]
public sealed partial class RandomPlantMutationListPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Whether mutation odds from this table ignore mutationMod.
    /// </summary>
    [DataField]
    public bool IgnoreMutationMod;

    /// <summary>
    /// List of RandomFills that can be picked from.
    /// </summary>
    [DataField(required: true)]
    public List<RandomPlantMutation> Mutations = [];
}
