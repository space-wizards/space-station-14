using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Components;

/// <summary>
/// A table of plant mutation entries and their individual odds.
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
    public List<RandomPlantMutationEntry> Mutations = [];
}
