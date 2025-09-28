using System.Linq;
using Robust.Shared.Collections;
using Content.Shared.Body.Systems;
using Content.Shared.Popups;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Random.Helpers;
using Robust.Shared.Network;

namespace Content.Shared.Medical.Disease;

/// <summary>
/// Server system that progresses diseases, triggers symptom behaviors, and handles spread/immunity.
/// </summary>
public sealed partial class SharedDiseaseSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedDiseaseSymptomSystem _symptoms = default!;
    [Dependency] private readonly SharedDiseaseCureSystem _cure = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedInternalsSystem _internals = default!;

    /// <inheritdoc/>
    /// <summary>
    /// Processes carriers on their scheduled ticks.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DiseaseCarrierComponent>();
        var now = _timing.CurTime;
        var carriersToProcess = new List<(EntityUid uid, DiseaseCarrierComponent carrier)>();

        while (query.MoveNext(out var uid, out var carrier))
        {
            if (carrier.NextTick > now)
                continue;

            carrier.NextTick = now + carrier.TickDelay;
            Dirty(uid, carrier);
            carriersToProcess.Add((uid, carrier));
        }

        foreach (var (uid, carrier) in carriersToProcess)
        {
            ProcessCarrier((uid, carrier));
        }
    }

    /// <summary>
    /// Advances disease stages and triggers symptom behaviors when eligible.
    /// Removes invalid diseases.
    /// </summary>
    private void ProcessCarrier(Entity<DiseaseCarrierComponent> ent)
    {
        if (ent.Comp.ActiveDiseases.Count == 0)
        {
            if (!string.IsNullOrEmpty(ent.Comp.DiseaseIcon))
            {
                ent.Comp.DiseaseIcon = string.Empty;
                Dirty(ent);
            }
            return;
        }

        var dirty = false;
        var toRemove = new ValueList<string>();

        foreach (var (diseaseId, stage) in ent.Comp.ActiveDiseases.ToArray())
        {
            if (!_prototypes.TryIndex(diseaseId, out DiseasePrototype? disease))
            {
                toRemove.Add(diseaseId);
                continue;
            }

            // Incubation: if still incubating, skip symptoms and spreading-level logic.
            if (ent.Comp.IncubatingUntil.TryGetValue(diseaseId, out var until) && until > _timing.CurTime)
                continue;

            // Progression: scale advance chance strictly according to StageProb and time between ticks.
            var newStage = AdvanceStage(ent, disease, stage);

            if (newStage != stage)
            {
                ent.Comp.ActiveDiseases[diseaseId] = newStage;
                dirty = true;
            }

            // Trigger configured stage effects.
            TriggerStage(ent, disease, newStage);

            // Attempt passive cure steps for this disease.
            _cure.TriggerCureSteps(ent, disease);
        }

        foreach (var id in toRemove)
        {
            ent.Comp.ActiveDiseases.Remove(id);
            dirty = true;
        }

        // Update HUD icon.
        UpdateIcon(ent);

        if (dirty)
            Dirty(ent);
    }

    private int AdvanceStage(Entity<DiseaseCarrierComponent> ent, DiseasePrototype disease, int currentStage)
    {
        var perTickAdvance = Math.Clamp(disease.StageProb, 0f, 1f);
        var maxStage = Math.Max(1, disease.Stages.Count);
        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id, 1, currentStage });
        var rand = new System.Random(seed);
        if (rand.Prob(perTickAdvance))
            return Math.Min(currentStage + 1, maxStage);

        return currentStage;
    }

    private void TriggerStage(Entity<DiseaseCarrierComponent> ent, DiseasePrototype disease, int stage)
    {
        var stageCfg = disease.Stages.FirstOrDefault(s => s.Stage == stage);
        if (stageCfg == null)
            return;

        // Stage sensations: each entry has its own per-tick probability.
        for (var i = 0; i < stageCfg.Sensations.Count; i++)
        {
            var entry = stageCfg.Sensations[i];
            // TODO: Replace with RandomPredicted once the engine PR is merged
            var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id, 2, stage, i });
            var rand = new System.Random(seed);
            if (!rand.Prob(entry.Probability))
                continue;

            var text = Loc.GetString(entry.Sensation);
            _popup.PopupPredicted(text, ent, ent.Owner, entry.PopupType);
            break;
        }

        // Symptoms are a list of detailed entries (symptom + optional probability override).
        for (var i = 0; i < stageCfg.Symptoms.Count; i++)
        {
            var entry = stageCfg.Symptoms[i];
            var symptomId = entry.Symptom;
            if (!_prototypes.TryIndex(symptomId, out DiseaseSymptomPrototype? symptom))
                continue;

            // Skip if this symptom is currently suppressed by a symptom-level cure.
            if (ent.Comp.SuppressedSymptoms.TryGetValue(symptomId, out var value) && value > _timing.CurTime)
                continue;

            var prob = entry.Probability >= 0f ? entry.Probability : symptom.Probability;
            // TODO: Replace with RandomPredicted once the engine PR is merged
            var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id, 3, stage, i });
            var rand = new System.Random(seed);
            if (!rand.Prob(prob))
                continue;

            _symptoms.TriggerSymptom(ent, disease, symptom);
        }
    }

    private void UpdateIcon(Entity<DiseaseCarrierComponent> ent)
    {
        // Collect present icon types from active diseases.
        var present = new HashSet<DiseaseIconType>();
        foreach (var (id, _) in ent.Comp.ActiveDiseases)
        {
            if (_prototypes.TryIndex<DiseasePrototype>(id, out var diseaseProto))
                present.Add(diseaseProto.IconType);
        }

        // Choose the first icon by priority defined in DiseaseHud.HudIcons.
        var selected = string.Empty;
        foreach (var (type, protoId) in DiseaseHud.HudIcons)
        {
            if (present.Contains(type))
            {
                selected = protoId;
                break;
            }
        }

        if (ent.Comp.DiseaseIcon != selected)
        {
            ent.Comp.DiseaseIcon = selected;
            Dirty(ent);
        }
    }

    /// <summary>
    /// Helper: finds an entity in a specific flagged slot if present.
    /// </summary>
    private bool TryGetInventoryEntity(EntityUid target, SlotFlags flags, out EntityUid item)
    {
        var enumerator = _inventory.GetSlotEnumerator((target, CompOrNull<InventoryComponent>(target)), flags);
        if (enumerator.NextItem(out item))
            return true;
        item = default;
        return false;
    }

    /// <summary>
    /// Adjusts airborne infection chance for PPE/internals on the target.
    /// </summary>
    public float AdjustAirborneChanceForProtection(EntityUid target, float baseChance, DiseasePrototype disease)
    {
        var chance = baseChance;

        if (_internals.AreInternalsWorking(target))
            chance *= DiseaseEffectiveness.InternalsMultiplier;

        var permeability = MathF.Max(0f, disease.PermeabilityMod);
        foreach (var (slot, mult) in DiseaseEffectiveness.AirborneSlots)
        {
            if (slot == SlotFlags.MASK)
            {
                if (TryGetInventoryEntity(target, SlotFlags.MASK, out var maskUid))
                {
                    if (TryComp<MaskComponent>(maskUid, out var mask) && !mask.IsToggled)
                        chance *= MathF.Min(1f, mult * permeability);
                }
                continue;
            }

            if (TryGetInventoryEntity(target, slot, out _))
                chance *= MathF.Min(1f, mult * permeability);
        }

        return MathF.Max(0f, MathF.Min(1f, chance));
    }

    /// <summary>
    /// Adjusts contact infection chance for PPE on the target.
    /// </summary>
    public float AdjustContactChanceForProtection(EntityUid target, float baseChance, DiseasePrototype disease)
    {
        var chance = baseChance;
        var permeability = MathF.Max(0f, disease.PermeabilityMod);
        foreach (var (slot, mult) in DiseaseEffectiveness.ContactSlots)
        {
            if (TryGetInventoryEntity(target, slot, out _))
                chance *= MathF.Min(1f, mult * permeability);
        }

        return MathF.Max(0f, MathF.Min(1f, chance));
    }

    /// <summary>
    /// Validates if an entity can be infected with a particular disease (alive and prototype exists).
    /// </summary>
    public bool CanBeInfected(EntityUid uid, string diseaseId)
    {
        if (!_prototypes.HasIndex<DiseasePrototype>(diseaseId))
            return false;

        if (!TryComp<MobStateComponent>(uid, out var mobState) || mobState.CurrentState == MobState.Dead)
            return false;

        return true;
    }

    /// <summary>
    /// Rolls probability, validates eligibility, then infects.
    /// </summary>
    public bool TryInfectWithChance(EntityUid uid, string diseaseId, float probability, int startStage = 1)
    {
        if (!CanBeInfected(uid, diseaseId))
            return false;

        if (!_random.Prob(probability))
            return false;

        if (TryComp<DiseaseCarrierComponent>(uid, out var carrier) && carrier.Immunity.TryGetValue(diseaseId, out var immunityStrength))
        {
            // Roll against immunity strength.
            if (_random.Prob(immunityStrength))
                return false;
        }

        return Infect(uid, diseaseId, startStage);
    }

    /// <summary>
    /// Infects an entity if eligible, when it has a carrier component, and sets the initial stage.
    /// </summary>
    public bool Infect(EntityUid uid, string diseaseId, int startStage = 1)
    {
        if (!_prototypes.HasIndex<DiseasePrototype>(diseaseId))
            return false;

        if (!TryComp<DiseaseCarrierComponent>(uid, out var carrier))
            return false;

        // Only initialize stage and incubation when this disease is first added to the carrier.
        if (!carrier.ActiveDiseases.ContainsKey(diseaseId))
        {
            // Set initial stage.
            carrier.ActiveDiseases[diseaseId] = startStage;

            // Schedule incubation window if configured; during incubation symptoms/spread are suppressed.
            var proto = _prototypes.Index<DiseasePrototype>(diseaseId);
            if (proto.IncubationSeconds > 0)
                carrier.IncubatingUntil[diseaseId] = _timing.CurTime + TimeSpan.FromSeconds(proto.IncubationSeconds);
        }

        carrier.NextTick = _timing.CurTime + carrier.TickDelay;
        Dirty(uid, carrier);
        return true;
    }
}
