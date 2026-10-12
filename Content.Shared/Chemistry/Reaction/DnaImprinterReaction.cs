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
    [DataField]
    public ProtoId<ReagentPrototype> DnaSource;
    [DataField]
    public ProtoId<ReagentPrototype> DnaOutput;
    public override void React(IReadOnlyList<ReagentQuantity> reactants, List<ReagentQuantity> products)
    {
        List<(FixedPoint2 Quantity, string? Dna)> dnaQuantities = [];
        FixedPoint2 inputQuantity = 0;
        FixedPoint2 outputQuantity = 0;
        foreach (var reactant in reactants)
        {
            if (DnaSource != reactant.Reagent.Prototype)
                continue;
            string? dna = reactant.Reagent.Data?.OfType<DnaData>().FirstOrDefault()?.DNA ?? null;
            dnaQuantities.Add((reactant.Quantity, dna));
            inputQuantity += reactant.Quantity;
        }

        if (dnaQuantities.Count <= 0)
            return;


        for (var i = 0; i < products.Count; i++)
        {
            var product = products[i];
            if (product.Reagent.Prototype != DnaOutput) continue;
            outputQuantity += product.Quantity;
            products[i] = new ReagentQuantity(product.Reagent.Prototype, 0);  // remove the base product so we can multiple based on the proportions.
        }

        FixedPoint2 outputRatio = outputQuantity / inputQuantity;

        foreach (var dnaQuantity in dnaQuantities)
        {
            if (dnaQuantity.Dna == null)
            {
                products.Add(new ReagentQuantity(DnaOutput, dnaQuantity.Quantity * outputRatio));
            }
            else
            {
                products.Add(new ReagentQuantity(DnaOutput, dnaQuantity.Quantity * outputRatio,
                    new List<ReagentData> { new DnaData { DNA = dnaQuantity.Dna } }));
            }
        }


    }

}
