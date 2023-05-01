using Content.Server.Disease;
using Content.Server.Disease.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Disease;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;
/// <summary>
/// Infects a couple people
/// with a random disease that isn't super deadly
/// </summary>
public sealed class DiseaseOutbreakRule : StationEventSystem<DiseaseOutbreakRuleComponent>
{
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    /// <summary>
    /// Finds 2-5 random, alive entities that can host diseases
    /// and gives them a randomly selected disease.
    /// They all get the same disease.
    /// </summary>
    protected override void Started(EntityUid uid, DiseaseOutbreakRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        HashSet<EntityUid> stationsToNotify = new();
        List<DiseaseCarrierComponent> aliveList = new();
        foreach (var (carrier, mobState) in EntityQuery<DiseaseCarrierComponent, MobStateComponent>())
        {
            if (!_mobStateSystem.IsDead(mobState.Owner, mobState))
                aliveList.Add(carrier);
        }
        RobustRandom.Shuffle(aliveList);

        // We're going to filter the above out to only alive mobs. Might change after future mobstate rework
        var toInfect = RobustRandom.Next(2, 5);

        var diseaseName = RobustRandom.Pick(component.NotTooSeriousDiseases);

        if (!PrototypeManager.TryIndex(diseaseName, out DiseasePrototype? disease))
            return;

        // Now we give it to people in the list of living disease carriers earlier
        foreach (var target in aliveList)
        {
            if (toInfect-- == 0)
                break;

            _diseaseSystem.TryAddDisease(target.Owner, disease, target);

            var station = StationSystem.GetOwningStation(target.Owner);
            if(station == null) continue;
            stationsToNotify.Add((EntityUid) station);
        }
    }
}
