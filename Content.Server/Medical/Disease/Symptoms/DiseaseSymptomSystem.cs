using Content.Server.Medical.Disease.Systems;
using Content.Shared.Medical.Disease;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;

namespace Content.Server.Medical.Disease.Symptoms;

/// <summary>
/// Encapsulates symptom-side effects and secondary spread mechanics for diseases.
/// </summary>
public sealed partial class DiseaseSymptomSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly AirborneDiseaseSystem _airborneDisease = default!;

    /// <inheritdoc/>
    /// <summary>
    /// Executes the side-effects for a triggered symptom on a carrier.
    /// </summary>
    public void TriggerSymptom(Entity<DiseaseCarrierComponent> ent, DiseasePrototype disease, DiseaseSymptomPrototype symptom)
    {
        // Skip this symptom when the carrier is dead.
        if (symptom.OnlyWhenAlive && _mobState.IsDead(ent.Owner))
            return;

        var deps = _entitySystemManager.DependencyCollection;

        if (symptom.SingleBehavior && symptom.Behaviors.Count > 0)
        {
            // Run exactly one random behavior.
            var behavior = symptom.Behaviors[_random.Next(0, symptom.Behaviors.Count)];
            deps.InjectDependencies(behavior, oneOff: true);
            behavior.OnSymptom(ent.Owner, disease);
        }
        else
        {
            foreach (var behavior in symptom.Behaviors)
            {
                deps.InjectDependencies(behavior, oneOff: true);
                behavior.OnSymptom(ent.Owner, disease);
            }
        }

        // Apply configurable symptom-driven airborne burst.
        ApplyAirborneBurst(symptom, ent, disease);
    }

    /// <summary>
    /// Applies a single-shot airborne spread burst if configured.
    /// </summary>
    private void ApplyAirborneBurst(DiseaseSymptomPrototype symptom, Entity<DiseaseCarrierComponent> ent, DiseasePrototype disease)
    {
        var cfg = symptom.AirborneBurst;

        if (!disease.SpreadFlags.Contains(DiseaseSpreadFlags.Airborne))
            return;

        var range = disease.AirborneRange * MathF.Max(0.1f, cfg.RangeMultiplier);
        var mult = MathF.Max(0f, cfg.ChanceMultiplier);
        _airborneDisease.TryAirborneSpread(ent.Owner, disease, overrideRange: range, chanceMultiplier: mult);
    }
}
