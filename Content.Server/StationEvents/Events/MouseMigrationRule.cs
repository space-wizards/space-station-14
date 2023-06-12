using System.Linq;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.StationEvents.Events;

public sealed class MouseMigrationRule : StationEventSystem<MouseMigrationRuleComponent>
{
    [Dependency] private readonly StationSystem _stationSystem = default!;

    protected override void Started(EntityUid uid, MouseMigrationRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var targetStation = _stationSystem.GetStations().FirstOrNull();

        if (!TryComp(targetStation, out StationDataComponent? data))
        {
            Logger.Info("TargetStation not have StationDataComponent");
            return;
        }

        var modifier = GetSeverityModifier();

        var spawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent, TransformComponent>().ToList();

        var grids = data.Grids.ToHashSet();
        spawnLocations.RemoveAll(
            backupSpawnLoc =>
                backupSpawnLoc.Item2.GridUid.HasValue &&
                !grids.Contains(backupSpawnLoc.Item2.GridUid.Value));

        RobustRandom.Shuffle(spawnLocations);

        // sqrt so we dont get insane values for ramping events
        var spawnAmount = (int) (RobustRandom.Next(7, 15) * Math.Sqrt(modifier)); // A small colony of critters.

        for (var i = 0; i < spawnAmount && i < spawnLocations.Count - 1; i++)
        {
            var spawnChoice = RobustRandom.Pick(component.SpawnedPrototypeChoices);
            if (RobustRandom.Prob(Math.Min(0.01f * modifier, 1.0f)) || i == 0) //small chance for multiple, but always at least 1
                spawnChoice = "SpawnPointGhostRatKing";

            var coords = spawnLocations[i].Item2.Coordinates;
            Sawmill.Info($"Spawning mouse {spawnChoice} at {coords}");
            EntityManager.SpawnEntity(spawnChoice, coords);
        }
    }
}
