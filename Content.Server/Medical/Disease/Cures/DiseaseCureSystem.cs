using System;
using System.Linq;
using System.Collections.Generic;
using Content.Shared.Popups;
using Content.Shared.Medical.Disease;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Random;

namespace Content.Server.Medical.Disease.Cures;

public sealed partial class DiseaseCureSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc/>
    /// <summary>
    /// Executes a configured cure step via its polymorphic OnCure.
    /// </summary>
    private bool ExecuteCureStep(Entity<DiseaseCarrierComponent> ent, CureStep step, DiseasePrototype disease)
    {
        var deps = IoCManager.Resolve<IEntitySystemManager>().DependencyCollection;
        deps.InjectDependencies(step, oneOff: true);
        return step.OnCure(ent.Owner, disease);
    }

    /// <summary>
    /// Attempts to apply cure steps for a disease on the provided carrier.
    /// </summary>
    public void TriggerCureSteps(Entity<DiseaseCarrierComponent> ent, DiseasePrototype disease)
    {
        if (!ent.Comp.ActiveDiseases.TryGetValue(disease.ID, out var stageNum))
            return;

        var stageCfg = disease.Stages.FirstOrDefault(s => s.Stage == stageNum);
        if (stageCfg == null)
            return;

        var applicable = stageCfg.CureSteps.Count > 0 ? stageCfg.CureSteps : disease.CureSteps;
        var simpleSymptoms = stageCfg.Symptoms.Select(s => s.Symptom).ToList();

        // disease-level cures
        foreach (var step in applicable)
        {
            // Calculates the probability of treatment at each tick.
            if (!_random.Prob(Math.Clamp(step.CureChance, 0f, 1f)))
                continue;

            if (!ExecuteCureStep(ent, step, disease))
                continue;

            if (step.LowerStage)
            {
                if (ent.Comp.ActiveDiseases.TryGetValue(disease.ID, out var curStage) && curStage > 1)
                {
                    ent.Comp.ActiveDiseases[disease.ID] = curStage - 1;
                    Dirty(ent);
                }
            }
            else
            {
                ApplyCureDisease(ent, disease, simpleSymptoms);
            }
        }

        // symptom-level cures
        foreach (var entry in stageCfg.Symptoms)
        {
            var symptomId = entry.Symptom;
            if (!_prototypeManager.TryIndex<DiseaseSymptomPrototype>(symptomId, out var symptomProto))
                continue;

            // If symptom is currently suppressed (recently treated).
            if (ent.Comp.SuppressedSymptoms.TryGetValue(symptomId, out var until) && until > _timing.CurTime)
                continue;

            if (symptomProto.CureSteps.Count == 0)
                continue;

            foreach (var step in symptomProto.CureSteps)
            {
                if (!_random.Prob(Math.Clamp(step.CureChance, 0f, 1f)))
                    continue;

                if (ExecuteCureStep(ent, step, disease))
                    ApplyCureSymptom(ent, disease, symptomId);
            }
        }
    }

    /// <summary>
    /// Removes the disease, applies post-cure immunity.
    /// </summary>
    public void ApplyCureDisease(Entity<DiseaseCarrierComponent> ent, DiseasePrototype disease, IReadOnlyList<ProtoId<DiseaseSymptomPrototype>> stageSymptoms)
    {
        if (!ent.Comp.ActiveDiseases.ContainsKey(disease.ID))
            return;

        ent.Comp.ActiveDiseases.Remove(disease.ID);
        ApplyPostCureImmunity(ent.Comp, disease);

        _popup.PopupEntity(Loc.GetString("disease-cured"), ent, ent.Owner);

        NotifyDiseaseCured(ent, disease, stageSymptoms);
    }

    /// <summary>
    /// Suppresses the given symptom for its configured duration and notifies hooks.
    /// </summary>
    public void ApplyCureSymptom(Entity<DiseaseCarrierComponent> ent, DiseasePrototype disease, string symptomId)
    {
        if (!_prototypeManager.TryIndex<DiseaseSymptomPrototype>(symptomId, out var symptomProto))
            return;

        var duration = symptomProto.CureDuration;
        if (duration <= 0f)
            return;

        ent.Comp.SuppressedSymptoms[symptomId] = _timing.CurTime + TimeSpan.FromSeconds(duration);

        _popup.PopupEntity(Loc.GetString("disease-cured-symptom"), ent, ent.Owner);

        NotifySymptomCured(ent, disease, symptomId);
    }

    /// <summary>
    /// Writes or raises the immunity strength for the cured disease on the carrier.
    /// </summary>
    private void ApplyPostCureImmunity(DiseaseCarrierComponent comp, DiseasePrototype disease)
    {
        var strength = disease.PostCureImmunity;

        if (comp.Immunity.TryGetValue(disease.ID, out var existing))
            comp.Immunity[disease.ID] = MathF.Max(existing, strength);
        else
            comp.Immunity[disease.ID] = strength;
    }

    /// <summary>
    /// Invokes <see cref="SymptomBehavior.OnDiseaseCured"/> on behaviors for the symptoms present on the cured stage only.
    /// </summary>
    private void NotifyDiseaseCured(Entity<DiseaseCarrierComponent> ent, DiseasePrototype disease, IReadOnlyList<ProtoId<DiseaseSymptomPrototype>> stageSymptoms)
    {
        foreach (var symptomId in stageSymptoms)
        {
            if (!_prototypeManager.TryIndex<DiseaseSymptomPrototype>(symptomId, out var symptomProto))
                continue;

            foreach (var behavior in symptomProto.Behaviors)
                behavior.OnDiseaseCured(ent.Owner, disease);
        }
    }

    /// <summary>
    /// Invokes <see cref="SymptomBehavior.OnSymptomCured"/> for the behaviors of the cured symptom.
    /// </summary>
    private void NotifySymptomCured(Entity<DiseaseCarrierComponent> ent, DiseasePrototype disease, string symptomId)
    {
        if (!_prototypeManager.TryIndex<DiseaseSymptomPrototype>(symptomId, out var symptomProto))
            return;

        foreach (var behavior in symptomProto.Behaviors)
            behavior.OnSymptomCured(ent.Owner, disease, symptomId);
    }

    /// <summary>
    /// Runtime per-step state stored in the system.
    /// </summary>
    internal sealed class CureState
    {
        public float Ticker;
    }

    private readonly Dictionary<(EntityUid, string, CureStep), CureState> _cureStates = new();

    /// <summary>
    /// Retrieves the runtime state for the given (entity, disease, cure), creating it if missing.
    /// </summary>
    internal CureState GetState(EntityUid uid, string diseaseId, CureStep step)
    {
        var key = (uid, diseaseId, step);
        if (!_cureStates.TryGetValue(key, out var state))
        {
            state = new CureState();
            _cureStates[key] = state;
        }
        return state;
    }
}
