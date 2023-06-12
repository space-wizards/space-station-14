using Content.Server.StationEvents.Components;
using Robust.Shared.Random;
using System.Linq;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Robust.Shared.Utility;

namespace Content.Server.StationEvents.Events;

public sealed class VentCrittersRule : StationEventSystem<VentCrittersRuleComponent>
{
    /*
     * DO NOT COPY PASTE THIS TO MAKE YOUR MOB EVENT.
     * USE THE PROTOTYPE.
     */
    [Dependency] private readonly StationSystem _stationSystem = default!;

    protected override void Started(EntityUid uid, VentCrittersRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var targetStation = _stationSystem.GetStations().FirstOrNull();

        if (!TryComp(targetStation, out StationDataComponent? data))
        {
            Logger.Info("TargetStation not have StationDataComponent");
            return;
        }

        var spawnChoice = RobustRandom.Pick(component.Entries);
        // TODO: What we should actually do is take the component count and then multiply a prob by that
        // then just iterate until we get it
        // This will be on average twice as fast.
        var spawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent, TransformComponent>().ToList();

        var grids = data.Grids.ToHashSet();
        spawnLocations.RemoveAll(
            backupSpawnLoc =>
                backupSpawnLoc.Item2.GridUid.HasValue && !grids.Contains(backupSpawnLoc.Item2.GridUid.Value));

        RobustRandom.Shuffle(spawnLocations);

        // A small colony of critters.
        var spawnAmount = RobustRandom.Next(spawnChoice.Amount, spawnChoice.MaxAmount);
        Sawmill.Info($"Spawning {spawnAmount} of {spawnChoice}");
        foreach (var location in spawnLocations)
        {
            if (spawnAmount-- == 0)
                break;

            var coords = Transform(location.Item2.Owner);
            Spawn(spawnChoice.PrototypeId, coords.Coordinates);
        }
    }
}
