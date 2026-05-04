using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Metabolism;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Analyzers;

public sealed class VitalsAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly PerfusionSystem _perfusion = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MetabolizerSystem _metabolizer = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;

    private const string MedicineGroup = "Medicine";
    private static readonly ProtoId<MetabolismStagePrototype> MetabolitesStage = "Metabolites";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VitalsAnalyzerComponent, AnalyzerUpdatedEvent>(OnAnalyzerUpdated);
    }

    private void OnAnalyzerUpdated(Entity<VitalsAnalyzerComponent> ent, ref AnalyzerUpdatedEvent args)
    {
        ent.Comp.Data = TakeSample(args.Target);
        Dirty(ent);
    }

    private Dictionary<ProtoId<ReagentPrototype>, (FixedPoint2 InBloodstream, FixedPoint2 Metabolites)>? SampleReagents(EntityUid uid, out bool hasNonMedical)
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

        return ret;
    }

    public VitalsData? TakeSample(EntityUid uid, bool withWounds = true)
    {
        if (!TryComp<PerfusionComponent>(uid, out var heartrate))
            return null;

        if (!TryComp<BrainDamageThresholdsComponent>(uid, out var brainDamageThresholds))
            return null;

        var (upper, lower) = _perfusion.BloodPressure((uid, heartrate));

        var hasNonMedical = false;
        var reagents = withWounds ? SampleReagents(uid, out hasNonMedical) : null;

        return new VitalsData()
            {
                BrainHealth = 1f - brainDamageThresholds.DisplayDamage.Float() / brainDamageThresholds.DisplayMaxDamage.Float(),
                BloodPressure = (upper, lower),
                HeartRate = _perfusion.HeartRate((uid, heartrate)),
                Etco2 = _perfusion.Etco2((uid, heartrate)),
                RespiratoryRate = _perfusion.RespiratoryRate((uid, heartrate)),
                Spo2 = _perfusion.Spo2((uid, heartrate)).Float(),
                Etco2Name = heartrate.Etco2Name,
                Etco2GasName = heartrate.Etco2GasName,
                Spo2Name = heartrate.Spo2Name,
                Spo2GasName = heartrate.Spo2GasName,
                Reagents = reagents,
                NonMedicalReagents = hasNonMedical,
                BloodLevel = _bloodstream.GetBloodLevel(uid),
            };
    }
}
