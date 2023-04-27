using System.Linq;
using Content.Server.StationEvents.Components;
using Content.Shared.Humanoid.Markings;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;

namespace Content.Server.StationEvents.Events;

public sealed class CluwneBeastSpawnRule : StationEventSystem<CluwneBeastSpawnRuleComponent>
{

    protected override void Started(EntityUid uid, CluwneBeastSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        var spawnLocations = EntityQuery<VentCritterSpawnLocationComponent>().ToList();
        RobustRandom.Shuffle(spawnLocations);

        var mod = Math.Sqrt(GetSeverityModifier());

        var spawnAmount = (component.SpawnCluwneBeast);
        Sawmill.Info($"Spawning {spawnAmount} cluwnebeast(s)");
        foreach (var location in spawnLocations)
        {
            if (spawnAmount-- == 0)
                break;

            var xform = Transform(location.Owner);

            Spawn(component.GhostSpawnPoint, xform.Coordinates);
        }
    }
}

