using Content.Server.Body.Components;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server._Offbrand.Wounds;

public sealed class WoundableHealthAnalyzerSystem : SharedWoundableHealthAnalyzerSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override Dictionary<ProtoId<ReagentPrototype>, (FixedPoint2 InBloodstream, FixedPoint2 Metabolites)>? SampleReagents(EntityUid uid, out bool hasNonMedical)
    {
        hasNonMedical = false;
        if (!TryComp<BloodstreamComponent>(uid, out var bloodstream))
            return null;

        if (!_solutionContainer.ResolveSolution(uid, bloodstream.ChemicalSolutionName, ref bloodstream.ChemicalSolution))
            return null;

        var ret = new Dictionary<ProtoId<ReagentPrototype>, (FixedPoint2 InBloodstream, FixedPoint2 Metabolites)>();

        foreach (var (reagentId, quantity) in bloodstream.ChemicalSolution.Value.Comp.Solution.Contents)
        {
            ProtoId<ReagentPrototype> reagent = reagentId.Prototype;

            if (_prototype.Index(reagent).Group != MedicineGroup)
            {
                hasNonMedical = true;
                continue;
            }

            if (!ret.ContainsKey(reagent))
                ret[reagent] = (0, 0);

            ret[reagent] = (ret[reagent].InBloodstream + quantity, ret[reagent].Metabolites);
        }

        foreach (var metabolizer in _body.GetBodyOrganEntityComps<MetabolizerComponent>(uid))
        {
            if (metabolizer.Comp1.SolutionName != bloodstream.ChemicalSolutionName)
                continue;

            foreach (var (reagent, quantity) in metabolizer.Comp1.Metabolites)
            {
                if (_prototype.Index(reagent).Group != MedicineGroup)
                {
                    hasNonMedical = true;
                    continue;
                }

                if (!ret.ContainsKey(reagent))
                    ret[reagent] = (0, 0);

                ret[reagent] = (ret[reagent].InBloodstream, ret[reagent].Metabolites + quantity);
            }
        }

        return ret;
    }
}
