namespace Content.Shared.Botany.Components;

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
