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
        var input = FixedPoint2.Zero;
        foreach (var reactant in reactants)
        {
            if (reactant.Reagent.Prototype == DnaSource)
                input += reactant.Quantity;
        }

        if (input <= FixedPoint2.Zero)
            return;

        var output = FixedPoint2.Zero;
        for (var i = products.Count - 1; i >= 0; i--)
        {
            if (products[i].Reagent.Prototype != DnaOutput)
                continue;

            output += products[i].Quantity;
            products.RemoveAt(i);
        }

        if (output <= FixedPoint2.Zero)
            return;

        var ratio = output / input;
        foreach (var reactant in reactants)
        {
            if (reactant.Reagent.Prototype != DnaSource)
                continue;

            List<ReagentData>? data = null;
            if (reactant.Reagent.Data?.OfType<DnaData>().FirstOrDefault() is { } dna)
                data = new List<ReagentData>{ dna.Clone() };

            products.Add(new ReagentQuantity(DnaOutput, reactant.Quantity * ratio, data));
        }
    }
}
