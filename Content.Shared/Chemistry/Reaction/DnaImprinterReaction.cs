using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reaction;

/// <summary>
/// Copies the most abundant donor's DNA from a recipe's blood ingredient to its products.
/// </summary>
public sealed partial class DnaImprinterReaction : ReactionExtension
{
    public override void React(IReadOnlyList<ReagentQuantity> reactants, List<ReagentQuantity> products)
    {
        string? dna = null;
        foreach (var reactant in reactants)
        {
            var dnaData = reactant.Reagent.Data?.OfType<DnaData>().FirstOrDefault();
            if (dnaData == null)
                continue;
            dna = dnaData.DNA;
        }

        if (dna == null)
            return;

        for (var i = 0; i < products.Count; i++)
        {
            var product = products[i];
            var data = product.Reagent.EnsureReagentData();
            data.Add(new DnaData { DNA = dna });
            products[i] = new ReagentQuantity(product.Reagent.Prototype, product.Quantity, data);
        }
    }

}
