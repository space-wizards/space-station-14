using System;
using Content.Shared.Interaction;
using Content.Shared.Medical.Disease;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Medical.Disease.Systems;

/// <summary>
/// Handles airborne disease spread in a periodic.
/// Also exposes a helper for symptom-driven airborne bursts.
/// </summary>
public sealed class AirborneDiseaseSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly DiseaseSystem _disease = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<DiseaseCarrierComponent>();
        while (query.MoveNext(out var uid, out var carrier))
        {
            if (carrier.ActiveDiseases.Count == 0)
                continue;

            // Piggyback on the disease tick cadence to avoid extra scheduling overhead.
            if (carrier.NextTick > now)
                continue;

            foreach (var (diseaseId, _) in carrier.ActiveDiseases)
            {
                if (!_prototypes.TryIndex<DiseasePrototype>(diseaseId, out var disease))
                    continue;

                if (!disease.SpreadFlags.Contains(DiseaseSpreadFlags.Airborne))
                    continue;

                if (!_random.Prob(disease.AirborneTickChance))
                    continue;

                TryAirborneSpread(uid, disease);
            }
        }
    }

    /// <summary>
    /// Performs a one-off airborne spread attempt from a source carrier using disease parameters.
    /// </summary>
    public void TryAirborneSpread(EntityUid source, DiseasePrototype disease, float? overrideRange = null, float chanceMultiplier = 1f)
    {
        if (Deleted(source))
            return;

        var mapPos = _transformSystem.GetMapCoordinates(source);
        if (mapPos.MapId == MapId.Nullspace)
            return;

        var range = overrideRange ?? disease.AirborneRange;
        var targets = _lookup.GetEntitiesInRange(mapPos, range, LookupFlags.Dynamic | LookupFlags.Sundries);
        foreach (var other in targets)
        {
            if (other == source)
                continue;

            // Simple LOS/obstacle check, similar to atmospheric blocking behavior.
            if (!_transformSystem.InRange(source, other, range)
                || !EntityManager.TryGetComponent(source, out TransformComponent? srcXform)
                || !EntityManager.TryGetComponent(other, out TransformComponent? _))
                continue;

            // Try to avoid through-walls spread.
            if (!_interaction.InRangeUnobstructed(source, other, range))
                continue;

            var chance = Math.Clamp(disease.AirborneInfect * chanceMultiplier, 0f, 1f);
            chance = _disease.AdjustAirborneChanceForProtection(other, chance, disease);
            _disease.TryInfectWithChance(other, disease.ID, chance);
        }
    }
}


