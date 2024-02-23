using Content.Shared.Botany.Systems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Component for plant entities that set reagents in plants, scaled by potency.
/// </summary>
[RegisterComponent, Access(typeof(PlantChemicalsSystem))]
public sealed partial class PlantChemicalsComponent : Component
{
    /// <summary>
    /// Quantity data for each reagent present.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<ReagentPrototype>, PlantChemQuantity> Chemicals = new();

    /// <summary>
    /// Number to scale chemical quantities by.
    /// </summary>
    [DataField(required: true)]
    public float Potency;

    /// <summary>
    /// Solution to set reagents of in the produce.
    /// </summary>
    [DataField]
    public string Solution = "food";
}

[DataDefinition]
public partial struct PlantChemQuantity
{
    /// <summary>
    /// Minimum amount of chemical that is added to produce, regardless of the potency.
    /// </summary>
    [DataField(required: true)]
    public int Min;

    /// <summary>
    /// Maximum amount of chemical that can be produced after taking plant potency into account.
    /// </summary>
    [DataField(required: true)]
    public int Max;

    /// <summary>
    /// When chemicals are added to produce, the potency of the seed is divided with this value.
    /// Final chemical amount is the result plus <c>Min</c>.
    ///
    /// Example: PotencyDivisor of 20 with seed potency of 55 results in 2.75, 55/20 = 2.75.
    /// If minimum is 1 then final result will be 3.75 of that chemical, 55/20+1 = 3.75.
    /// </summary>
    [DataField(required: true)]
    public int PotencyDivisor;

    /// <summary>
    /// Inherent chemical is one that is NOT result of mutation or crossbreeding.
    /// These chemicals are removed if species gets mutated.
    /// </summary>
    [DataField]
    public bool Inherent = true;
}
