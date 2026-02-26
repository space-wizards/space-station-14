using Content.Server.Atmos.EntitySystems;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Body;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Metabolism;
using Robust.Shared.Prototypes;

namespace Content.Server._Offbrand.Wounds;

public sealed class WoundableHealthAnalyzerSystem : SharedWoundableHealthAnalyzerSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MetabolizerSystem _metabolizer = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    private static readonly ProtoId<MetabolismStagePrototype> MetabolitesStage = "Metabolites";

    public override Dictionary<ProtoId<ReagentPrototype>, (FixedPoint2 InBloodstream, FixedPoint2 Metabolites)>? SampleReagents(EntityUid uid, out bool hasNonMedical)
    {
        hasNonMedical = false;
        if (!TryComp<BloodstreamComponent>(uid, out var bloodstream))
            return null;

        if (!_solutionContainer.ResolveSolution(uid, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution))
            return null;

        var ret = new Dictionary<ProtoId<ReagentPrototype>, (FixedPoint2 InBloodstream, FixedPoint2 Metabolites)>();
        var reference = bloodstream.BloodReferenceSolution;

        foreach (var (reagentId, quantity) in bloodstream.BloodSolution.Value.Comp.Solution.Contents)
        {
            if (reference.ContainsPrototype(reagentId.Prototype))
                continue;

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

        _body.TryGetOrgansWithComponent<MetabolizerComponent>(uid, out var metabolizers);
        foreach (var metabolizer in metabolizers)
        {
            var solutions = metabolizer.Comp.Solutions;
            var stages = metabolizer.Comp.Stages;
            if (!solutions.TryGetValue(MetabolitesStage, out var stage) || !stages.Contains(MetabolitesStage))
                continue;

            if (!_metabolizer.LookupSolution(metabolizer, stage, false, out var metabolitesSolution, out _, out _))
                continue;

            foreach (var (reagent, quantity) in metabolitesSolution.Contents)
            {
                if (_prototype.Index<ReagentPrototype>(reagent.Prototype).Group != MedicineGroup)
                {
                    hasNonMedical = true;
                    continue;
                }

                if (!ret.ContainsKey(reagent.Prototype))
                    ret[reagent.Prototype] = (0, 0);

                ret[reagent.Prototype] = (ret[reagent.Prototype].InBloodstream, ret[reagent.Prototype].Metabolites + quantity);
            }
        }

        _body.TryGetOrgansWithComponent<LungComponent>(uid, out var lungs);
        foreach (var lung in lungs)
        {
            foreach (var gasId in Enum.GetValues<Gas>())
            {
                var idx = (int) gasId;
                var moles = lung.Comp.Air[idx];
                if (moles <= 0)
                    continue;

                if (_atmosphere.GasReagents[idx] is not { } reagent)
                    continue;

                var amount = FixedPoint2.New(moles * Atmospherics.BreathMolesToReagentMultiplier);
                if (amount <= 0)
                    continue;

                if (!ret.ContainsKey(reagent))
                    ret[reagent] = (0, 0);

                ret[reagent] = (ret[reagent].InBloodstream + amount, ret[reagent].Metabolites);
            }
        }

        return ret;
    }
}
