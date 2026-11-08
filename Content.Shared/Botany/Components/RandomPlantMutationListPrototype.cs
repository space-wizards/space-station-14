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
    /// List of random mutations that can be picked from.
    /// </summary>
    [DataField(required: true)]
    public List<RandomPlantMutationEntry> Mutations = [];
}

/// <summary>
/// A mutation table entry containing its odds and mutation overrides.
/// </summary>
[DataDefinition]
public sealed partial class RandomPlantMutationEntry
{
    /// <summary>
    /// Independent odds per point of mutation severity, rather than a relative selection weight.
    /// </summary>
    [DataField]
    public float BaseOdds;

    [DataField(required: true)]
    public RandomPlantMutationOverride Mutation = default!;
}
