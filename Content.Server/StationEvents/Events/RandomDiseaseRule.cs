using Content.Server.Medical.Disease.Systems;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Medical.Disease;
using Content.Shared.Mind.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

/// <summary>
///     Station event that infects a random selection of suitable players with a randomly chosen disease from a configured pool.
///     Mirrors the structure/pattern of other events like RandomSentienceRule.
/// </summary>
public sealed class RandomDiseaseRule : StationEventSystem<RandomDiseaseRuleComponent>
{
    [Dependency] private readonly DiseaseSystem _disease = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Started(EntityUid uid, RandomDiseaseRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        if (comp.Disease.Count == 0)
            return;

        if (!TryGetRandomStation(out var station))
            return;

        // Choose disease uniformly from pool.
        var chosenDisease = _random.Pick(comp.Disease);

        // Collect eligible humanoids with carrier component on the chosen station.
        var candidates = new List<EntityUid>();
        var query = EntityQueryEnumerator<DiseaseCarrierComponent, MindContainerComponent, HumanoidAppearanceComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out _, out var mind, out _, out var xform))
        {
            if (StationSystem.GetOwningStation(ent, xform) != station)
                continue;

            // Central eligibility check: prototype exists, not dead.
            if (!_disease.CanBeInfected(ent, chosenDisease))
                continue;

            // Only consider entities with an attached mind (players).
            if (!mind.HasMind)
                continue;

            candidates.Add(ent);
        }

        if (candidates.Count == 0)
            return;

        // Determine how many to infect.
        var toInfect = Math.Clamp(_random.Next(comp.MinInfections, comp.MaxInfections + 1), 0, candidates.Count);
        _random.Shuffle(candidates);

        var infected = 0;
        foreach (var ent in candidates)
        {
            if (infected >= toInfect)
                break;

            // Optional: skip entities already immune if desired.
            if (comp.SkipImmune)
            {
                if (TryComp<DiseaseCarrierComponent>(ent, out var carrier) && carrier.Immunity.TryGetValue(chosenDisease, out var immunity) && immunity >= 1f)
                    continue;
            }

            if (_disease.Infect(ent, chosenDisease))
                infected++;
        }
    }
}


