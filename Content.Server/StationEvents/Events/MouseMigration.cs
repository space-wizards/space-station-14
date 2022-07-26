using System.Linq;
using Content.Server.Ghost.Roles;
using Content.Server.StationEvents.Components;
using Robust.Server.Player;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class MouseMigration : StationEventSystem, IGhostRoleRequester
{
    [Dependency] private GhostRoleSystem _ghostRoleSystem = default!;

    public static List<string> SpawnedPrototypeChoices = new List<string>() //we double up for that ez fake probability
        {"MobMouse", "MobMouse1", "MobMouse2", "MobRatServant"};

    public override string Prototype => "MouseMigration";

    public override void Started()
    {
        base.Started();

        var spawnAmount = RobustRandom.Next(7, 15); // A small colony of critters.
        _ghostRoleSystem.RegisterRequest(this, 7, spawnAmount, TimeSpan.FromMinutes(1));
    }

    public void OnRequestComplete(IEnumerable<IPlayerSession>? sessions)
    {
        if (sessions == null)
            return;

        var spawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent, TransformComponent>().ToList();
        RobustRandom.Shuffle(spawnLocations);

        var i = 0;
        foreach (var session in sessions)
        {
            var spawnChoice = RobustRandom.Pick(SpawnedPrototypeChoices);
            if (RobustRandom.Prob(0.01f) || i == 0) //small chance for multiple, but always at least 1
                spawnChoice = "MobRatKing";

            var coords = spawnLocations[i].Item2.Coordinates;
            Sawmill.Info($"Spawning mouse {spawnChoice} at {coords}");
            EntityManager.SpawnEntity(spawnChoice, coords);

            // TODO: Mind takeover.

            i++;
        }
    }
}
