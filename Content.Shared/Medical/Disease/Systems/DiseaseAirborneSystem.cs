using Content.Shared.Interaction;
using Content.Shared.Medical.Disease;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.Disease;

/// <summary>
/// Handles airborne disease spread in a periodic.
/// Also exposes a helper for symptom-driven airborne bursts.
/// </summary>
public sealed class DiseaseAirborneSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedDiseaseSystem _disease = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private readonly HashSet<EntityUid> _tmpTargets = [];

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

            if (carrier.NextTick > now)
                continue;

            foreach (var (diseaseId, _) in carrier.ActiveDiseases)
            {
                if (!_prototypes.TryIndex(diseaseId, out DiseasePrototype? disease))
                    continue;

                if ((disease.SpreadFlags & DiseaseSpreadFlags.Airborne) == 0)
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
        _tmpTargets.Clear();

        // If the source is inside a container, include contained entities in the lookup.
        var sourceContained = _container.IsEntityOrParentInContainer(source);
        var flags = sourceContained ? LookupFlags.All : LookupFlags.Uncontained;
        _lookup.GetEntitiesInRange(mapPos.MapId, mapPos.Position, range, _tmpTargets, flags);
        foreach (var other in _tmpTargets)
        {
            if (other == source)
                continue;

            if (!_disease.CanBeInfected(other, disease.ID))
                continue;

            // If the source is contained, only allow infection within the same container hierarchy.
            if (sourceContained &&
                !_container.IsInSameOrParentContainer((source, null, null), (other, null, null)))
            {
                continue;
            }

            // Compute final chance.
            var chance = Math.Clamp(disease.AirborneInfect * chanceMultiplier, 0f, 1f);
            chance = _disease.AdjustAirborneChanceForProtection(other, chance, disease);
            if (chance <= 0f)
                continue;

            // Try to avoid through-walls spread.
            if (!_interaction.InRangeUnobstructed(source, other, range))
                continue;

            _disease.TryInfectWithChance(other, disease.ID, chance);
        }
    }
}


