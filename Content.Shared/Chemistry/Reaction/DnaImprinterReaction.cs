using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reaction;

/// <summary>
/// Copies the DNA from a recipe's ingredients to its products.
/// </summary>
public sealed partial class DnaImprinterReaction : ReactionExtension
{
    /// <summary>
    /// The consumed reagent that carries the DNA.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> DnaSource;

    /// <summary>
    /// The product that receives the DNA.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> DnaOutput;

    public override void React(IReadOnlyList<ReagentQuantity> reactants, List<ReagentQuantity> products)
    {
        var dnaQuantities = new List<(FixedPoint2 Quantity, string? Dna)>();
        FixedPoint2 inputQuantity = 0;
        FixedPoint2 outputQuantity = 0;

        foreach (var reactant in reactants)
        {
            if (DnaSource != reactant.Reagent.Prototype)
                continue;

            var dna = reactant.Reagent.Data?.OfType<DnaData>().FirstOrDefault()?.DNA;
            dnaQuantities.Add((reactant.Quantity, dna));
            inputQuantity += reactant.Quantity;
        }

        if (dnaQuantities.Count <= 0)
            return;

        for (var i = 0; i < products.Count; i++)
        {
            var product = products[i];
            if (product.Reagent.Prototype != DnaOutput)
                continue;

            outputQuantity += product.Quantity;
            products[i] = new ReagentQuantity(product.Reagent.Prototype, FixedPoint2.Zero); // remove the base product so we can multiple based on the proportions.
        }

        if (outputQuantity <= FixedPoint2.Zero)
            return;

        var outputRatio = outputQuantity / inputQuantity;
        foreach (var (quantity, dna) in dnaQuantities)
        {
            var outputAmount = quantity * outputRatio;
            if (dna == null)
                products.Add(new ReagentQuantity(DnaOutput, outputAmount));
            else
                products.Add(new ReagentQuantity(DnaOutput, outputAmount, new List<ReagentData>{ new DnaData { DNA = dna } }));
        }
    }
}
