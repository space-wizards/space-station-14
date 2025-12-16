using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.Botany.Components;

/// <summary>
/// Defines what reagents harvested produce will contain for this plant species.
/// </summary>
[RegisterComponent]
[DataDefinition]
public sealed partial class PlantChemicalsComponent : Component
{
    /// <summary>
    /// Mapping reagent id -> chemical info for this plant species.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, PlantChemQuantity> Chemicals = [];
}

[DataDefinition]
public partial struct PlantChemQuantity
{
    /// <summary>
    /// Minimum amount of chemical that is added to produce, regardless of the potency
    /// </summary>
    [DataField]
    public FixedPoint2 Min = FixedPoint2.Epsilon;

    /// <summary>
    /// Maximum amount of chemical that can be produced after taking plant potency into account.
    /// </summary>
    [DataField]
    public FixedPoint2 Max;

    /// <summary>
    /// When chemicals are added to produce, the potency of the seed is divided with this value. Final chemical amount is the result plus the `Min` value.
    /// Example: PotencyDivisor of 20 with seed potency of 55 results in 2.75, 55/20 = 2.75. If minimum is 1 then final result will be 3.75 of that chemical, 55/20+1 = 3.75.
    /// </summary>
    [DataField]
    public float PotencyDivisor;

    /// <summary>
    /// Inherent chemical is one that is NOT result of mutation or crossbreeding. These chemicals are removed if species mutation is executed.
    /// </summary>
    [DataField]
    public bool Inherent = true;
}
