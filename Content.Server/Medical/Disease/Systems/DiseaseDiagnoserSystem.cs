using Content.Shared.Medical.Disease;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Paper;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Content.Server.Popups;
using Robust.Shared.Localization;

namespace Content.Server.Medical.Disease.Systems;

/// <summary>
/// Handles using a DiseaseSample on the DiseaseDiagnoser to print a report.
/// TODO: there should be more functions.
/// </summary>
public sealed class DiseaseDiagnoserSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PaperSystem _paper = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DiseaseDiagnoserComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, DiseaseDiagnoserComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<DiseaseSampleComponent>(args.Used, out var sample))
            return;

        // Reject if no sample material present
        if (!sample.HasSample)
        {
            _popup.PopupEntity(Loc.GetString("diagnoser-disease-empty-swab-popup"), uid, args.User);
            args.Handled = true;
            return;
        }

        args.Handled = true;

        // Build report text
        var content = BuildReportContent(sample);

        // Spawn paper and set content
        var paperUid = EntityManager.SpawnAtPosition("DiagnosisReportPaper", Transform(uid).Coordinates);
        if (TryComp<PaperComponent>(paperUid, out var paperComp))
        {
            _paper.SetContent((paperUid, paperComp), content);
        }

        // Clear diagnoser state if any and clear sample to avoid reusing stale data
        sample.Diseases.Clear();
        sample.Stages.Clear();
        sample.SubjectName = null;
        sample.SubjectDNA = null;
        sample.HasSample = false;

        _popup.PopupEntity(Loc.GetString("diagnoser-disease-printed-popup"), uid, args.User);
    }

    private string BuildReportContent(DiseaseSampleComponent sample)
    {
        var headerParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(sample.SubjectName))
            headerParts.Add(Loc.GetString("diagnoser-disease-report-subject", ("name", sample.SubjectName!)));

        if (!string.IsNullOrWhiteSpace(sample.SubjectDNA))
            headerParts.Add(Loc.GetString("diagnoser-disease-report-subject-dna", ("dna", sample.SubjectDNA!)));

        var header = string.Join("\n", headerParts);

        if (sample.Diseases.Count == 0)
        {
            var healthy = Loc.GetString("diagnoser-disease-report-none");
            return string.IsNullOrEmpty(header) ? healthy : $"{header}\n{healthy}";
        }

        var lines = new List<string>();
        foreach (var id in sample.Diseases)
        {
            if (!_prototypes.TryIndex<DiseasePrototype>(id, out var diseaseProto))
                continue;

            var displayName = Loc.GetString(diseaseProto.Name);
            var stage = sample.Stages.TryGetValue(id, out var s) ? s : 1;
            lines.Add(Loc.GetString("diagnoser-disease-report-name", ("name", displayName), ("stage", stage)));

            var description = Loc.GetString(diseaseProto.Description);
            lines.Add(Loc.GetString("diagnoser-disease-report-desc", ("desc", description)));

            DiseaseStage? stageCfg = null;
            foreach (var stCfg in diseaseProto.Stages)
            {
                if (stCfg.Stage == stage)
                {
                    stageCfg = stCfg;
                    break;
                }
            }

            if (stageCfg == null)
                continue;

            // Symptoms block
            lines.Add(Loc.GetString("diagnoser-disease-symptoms-header"));
            if (stageCfg.Symptoms.Count == 0)
            {
                lines.Add("- " + Loc.GetString("diagnoser-disease-symptoms-none"));
            }
            else
            {
                foreach (var symptomEntry in stageCfg.Symptoms)
                {
                    var symptomId = symptomEntry.Symptom;
                    var symName = "";
                    if (_prototypes.TryIndex<DiseaseSymptomPrototype>(symptomId, out var symProto))
                        symName = Loc.GetString(symProto.Name);

                    lines.Add("- " + symName);
                }
            }

            // Cures block
            var cureSteps = (stageCfg.CureSteps.Count > 0) ? stageCfg.CureSteps : diseaseProto.CureSteps;
            if (cureSteps.Count == 0)
            {
                lines.Add(Loc.GetString("diagnoser-no-cures"));
            }
            else
            {
                lines.Add(Loc.GetString("diagnoser-cure-has"));
                foreach (var step in cureSteps)
                {
                    foreach (var cureLine in step.BuildDiagnoserLines(_prototypes))
                    {
                        if (step.LowerStage)
                            lines.Add("- " + cureLine + " " + Loc.GetString("diagnoser-cure-lower-stage"));
                        else
                            lines.Add("- " + cureLine + " " + Loc.GetString("diagnoser-cure-lower-disease"));
                    }
                }
            }

            lines.Add("\n");
        }

        var body = string.Join("\n", lines);
        return string.IsNullOrEmpty(header) ? body : $"{header}\n{body}";
    }
}
