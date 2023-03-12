using System.Linq;
using Content.Server.StationEvents.Components;
using Content.Shared.Humanoid.Markings;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Configurations;

namespace Content.Server.StationEvents.Events;

public sealed class CluwneBeastSpawn : StationEventSystem
{
    public override string Prototype => "CluwneBeastSpawn";

    public override void Started()
    {
        base.Started();

        if (Configuration is not CluwneBeastRuleConfiguration config)
            return;

        var spawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent>().ToList();
        RobustRandom.Shuffle(spawnLocations);
        var mod = Math.Sqrt(GetSeverityModifier());
        var spawnAmount = (config.SpawnCluwneBeast);
        Sawmill.Info($"Spawning {spawnAmount} cluwnebeast(s)");
        foreach (var location in spawnLocations)
        {
            if (spawnAmount-- == 0)
                break;

            var coords = Transform(location.Owner);

            Spawn(config.GhostSpawnPoint, coords.Coordinates);
        }
    }
}
